using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public sealed class BattleCinematicDirector : MonoBehaviour
{
    private enum CinematicSequenceType
    {
        None = 0,
        PlayerAttack,
        MonsterAttack,
        PlayerParryAttack,
    }

    private readonly Dictionary<CinematicSequenceType, int> _cinematicSequenceIndices = new();

    private IBattleCinematicEventHandler _eventHandler;

    [Header("Actor Movement")]
    [SerializeField]
    private BattleActorMover _actorMover;

    [Header("Timeline")]
    [SerializeField]
    private PlayableDirector _battleStartDirector;

    [SerializeField]
    private PlayableDirector[] _playerAttackDirectors;

    [SerializeField]
    private PlayableDirector[] _monsterAttackDirectors;

    [SerializeField]
    private PlayableDirector[] _playerParryAttackDirectors;

    [SerializeField]
    private PlayableDirector _playerHitDirector;

    [SerializeField]
    private PlayableDirector _monsterHitDirector;

    private PlayableDirector _currentAttackDirector;

    [Header("Track Names")]
    [SerializeField]
    private string _playerAttackTrackName = "PlayerAnimationTrack";

    [SerializeField]
    private string _monsterAttackTrackName = "MonsterAnimationTrack";

    [SerializeField]
    private string _commonTrackName = "AnimationTrack";

    [Header("Camera")]
    [SerializeField]
    private BattleCameraRig _cameraRig;

    [SerializeField]
    private BattleCinematicShakeCamera _cinematicCamera;

    [Header("Hit Stop")]
    [SerializeField]
    private float _parryHitStopTimeScale = 0.15f;

    [SerializeField]
    private int _parryHitStopDurationMs = 100;

    private UniTaskCompletionSource _battleStartCompletionSource;

    /// <summary>
    /// 현재 피격 '당하는' 대상
    /// </summary>
    private PlayableDirector _currentHitDirector;

    private Func<BattleResult> _onImpact;
    private Func<BattleState> _onTurnEnd;
    private BattleResult _lastResult = BattleResult.None;
    private bool _isAttackImpactProcessed;
    private BattleTeam _currentAttackerTeam;
    private ActorBase _attackActor;
    private ActorBase _defendActor;
    private Vector3 _attackerOriginPosition;
    private Quaternion _attackerOriginRotation;

    public void BindEventHandler(IBattleCinematicEventHandler eventHandler)
    {
        _eventHandler = eventHandler;
    }

    public void BindActors(ActorBase player, ActorBase monster)
    {
        BindAnimator(_playerHitDirector, _commonTrackName, player);
        BindAnimator(_monsterHitDirector, _commonTrackName, monster);

        BindAnimators(_playerAttackDirectors, _playerAttackTrackName, player);
        BindAnimators(_monsterAttackDirectors, _monsterAttackTrackName, monster);
        BindAnimators(_playerParryAttackDirectors, _commonTrackName, player);
    }

    public async UniTask PlayBattleStartAsync()
    {
        if (_battleStartDirector == null)
        {
            return;
        }

        _battleStartCompletionSource = new UniTaskCompletionSource();

        _battleStartDirector.time = 0;
        _battleStartDirector.Evaluate();
        _battleStartDirector.Play();

        await _battleStartCompletionSource.Task;
    }

    public void PlayAttack(
        BattleTeam attackerTeam,
        ActorBase attacker,
        ActorBase defender,
        Action onApproachEnd,
        Func<BattleResult> onImpact,
        Func<BattleState> onTurnEnd)
    {
        _currentAttackDirector = GetAttackDirector(attackerTeam);
        PlayableDirector hitDirector = GetDefenderHitDirector(attackerTeam);

        _currentAttackerTeam = attackerTeam;
        _currentHitDirector = hitDirector;

        PlayAttackAsync(
            attackerTeam,
            attacker,
            defender,
            _currentAttackDirector,
            hitDirector,
            onApproachEnd,
            onImpact,
            onTurnEnd).Forget();
    }

    #region Timeline Signal
    public void OnBattleStartEndSignal()
    {
        _battleStartCompletionSource?.TrySetResult();
        _battleStartCompletionSource = null;
    }

    /// <summary>
    /// Timeline Signal과 BattleModel 판정을 연결하는 연출 진입점.
    /// 데미지 적용은 Timeline의 Impact 시점에만 실행하여
    /// 애니메이션 타이밍과 게임 로직 결과가 어긋나지 않도록 한다.
    /// </summary>
    public void OnAttackImpactSignal()
    {
        if (_isAttackImpactProcessed)
        {
            return;
        }

        _isAttackImpactProcessed = true;

        if (_onImpact == null)
        {
            Debug.LogWarning("[BattleCinematicDirector] Hit signal received, but applyHit callback is null.");
            return;
        }

        _lastResult = _onImpact();

        if (_lastResult.IsFinished)
        {
            return;
        }

        switch (_lastResult.ReactionType)
        {
            case DefenderReactionType.Hit:
            {
                PlayDirector(_currentHitDirector);
                break;
            }
            case DefenderReactionType.Parry:
            {
                _cameraRig?.PlayParrySuccessCamera(_defendActor, _attackActor);

                LookAtTargetFlat(_attackActor, _defendActor);
                LookAtTargetFlat(_defendActor, _attackActor);

                _eventHandler.OnParrySucceeded();

                ParryHitReactionAsync().Forget();

                PlayableDirector parryAttackDirector = GetNextDirector(CinematicSequenceType.PlayerParryAttack, _playerParryAttackDirectors);

                PlayDirector(parryAttackDirector);

                break;
            }
            default:
            {
                break;
            }
        }
    }

    public void OnAttackHitReactionSignal()
    {
        PlayDirector(_currentHitDirector);
    }

    public void OnAttackEndSignal()
    {
        if (_lastResult.IsFinished)
        {
            ClearCallbacks();
            return;
        }

        if (_lastResult.ReactionType == DefenderReactionType.Parry)
        {
            return;
        }

        AttackEnd().Forget();
    }

    public void OnParryEnableSignal()
    {
        if (_eventHandler == null)
        {
            return;
        }

        _eventHandler.OnParryWindowOpened();
    }

    public void OnParryDisableSignal()
    {
        if (_eventHandler == null)
        {
            return;
        }

        _eventHandler.OnParryWindowClosed();
    }

    public void OnParryImpactEffectSignal()
    {
        HitStopAnim(200).Forget();

        PlayHitStopAsync(_parryHitStopTimeScale, _parryHitStopDurationMs).Forget();

        _cinematicCamera?.PlayCounterImpact(GetBattleCenter());
    }

    public void OnParryEndSignal()
    {
        _attackActor.ResumeAnimator();
        _defendActor.ForceIdle();

        _eventHandler.OnParryEnd();
    }
    #endregion


    #region Private Sequence
    private async UniTask PlayAttackAsync(
        BattleTeam attackerTeam,
        ActorBase attacker,
        ActorBase defender,
        PlayableDirector attackDirector,
        PlayableDirector hitDirector,
        Action onApproachEnd,
        Func<BattleResult> onImpact,
        Func<BattleState> onTurnEnd)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogError("[BattleCinematicDirector] PlayAttackAsync failed. Attacker or defender is null.");
            return;
        }

        _attackerOriginPosition = attacker.transform.position;
        _attackerOriginRotation = attacker.transform.rotation;

        _attackActor = attacker;
        _defendActor = defender;

        _onImpact = onImpact;
        _onTurnEnd = onTurnEnd;

        _lastResult = BattleResult.None;

        _isAttackImpactProcessed = false;

        _cameraRig?.PlayMoveCamera(_currentAttackerTeam, attacker, defender);

        if (_actorMover == null)
        {
            Debug.LogError("[BattleCinematicDirector] BattleActorMover is null.");
            return;
        }

        await _actorMover.MoveToTargetAsync(
            attackerTeam,
            attacker.GetAnimator,
            attacker.transform,
            defender.transform);

        onApproachEnd?.Invoke();

        _cameraRig?.PlayAttackCamera(_currentAttackerTeam, attacker, defender);

        PlayDirector(attackDirector);
    }

    /// <summary>
    /// 패링 성공 시 몬스터 공격 타임라인은 종료되지만,
    /// 반격 시퀀스가 아직 진행 중이므로 복귀/턴 종료는 하지 않는다.
    /// 몬스터는 반격 피격 전까지 현재 자세로 경직시킨다.
    /// </summary>
    public async UniTask ParryHitReactionAsync()
    {
        if (_attackActor == null)
        {
            return;
        }

        _cinematicCamera?.PlayParryReaction(GetBattleCenter());

        _currentAttackDirector?.Stop();

        await HitStopAnim(100);
    }

    public async UniTask HitStopAnim(int stopTimeMs)
    {
        _attackActor.ResumeAnimator();

        _attackActor.GetAnimator.speed = 1f;
        _attackActor.GetAnimator.CrossFade("Hit", 0.03f, 0, 0f);

        await UniTask.Delay(stopTimeMs, ignoreTimeScale: true);

        _attackActor.PauseAnimator();
    }

    public async UniTask AttackEnd()
    {
        await _actorMover.ReturnAsync(_currentAttackerTeam, _attackActor.GetAnimator, _attackActor.transform, _attackerOriginPosition);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);

        _cameraRig?.ReturnToBase();

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }

    public async UniTask OnParryEndAsync()
    {
        await _actorMover.ReturnAsync(
            _currentAttackerTeam,
            _attackActor.GetAnimator,
            _attackActor.transform,
            _attackerOriginPosition);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);
        _cameraRig?.ReturnToBase();

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }

    private async UniTask PlayHitStopAsync(float timeScale, int durationMs)
    {
        float previousTimeScale = Time.timeScale;

        Time.timeScale = timeScale;

        await UniTask.Delay(
            durationMs,
            ignoreTimeScale: true);

        Time.timeScale = previousTimeScale;
    }
    #endregion


    #region Utility
    private void BindAnimators(PlayableDirector[] directors, string trackName, ActorBase actor)
    {
        if (directors == null)
        {
            return;
        }

        for (int i = 0; i < directors.Length; i++)
        {
            BindAnimator(directors[i], trackName, actor);
        }
    }

    private void BindAnimator(PlayableDirector director, string trackName, ActorBase actor)
    {
        if (director == null || actor == null)
        {
            return;
        }

        if (director.playableAsset == null)
        {
            Debug.LogError($"[BattleCinematicDirector] PlayableAsset is null. Director: {director.name}");
            return;
        }

        Animator animator = actor.GetAnimator;

        if (animator == null)
        {
            Debug.LogError($"[BattleCinematicDirector] Animator not found. Actor: {actor.name}");
            return;
        }

        foreach (PlayableBinding binding in director.playableAsset.outputs)
        {
            if (binding.streamName != trackName)
            {
                continue;
            }

            director.SetGenericBinding(binding.sourceObject, animator);
            return;
        }

        Debug.LogError($"[BattleCinematicDirector] Timeline track not found. TrackName: {trackName}");
    }

    private void PlayDirector(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.time = 0;
        director.Play();
    }

    private PlayableDirector GetNextDirector(CinematicSequenceType sequenceType, PlayableDirector[] directors)
    {
        if (directors == null || directors.Length == 0)
        {
            return null;
        }

        int sequenceIndex = 0;

        if (_cinematicSequenceIndices.ContainsKey(sequenceType))
        {
            sequenceIndex = _cinematicSequenceIndices[sequenceType];
        }

        PlayableDirector selectedDirector = directors[sequenceIndex];

        int nextIndex = sequenceIndex + 1;

        if (nextIndex >= directors.Length)
        {
            nextIndex = 0;
        }

        _cinematicSequenceIndices[sequenceType] = nextIndex;

        return selectedDirector;
    }

    private PlayableDirector GetAttackDirector(BattleTeam attackerTeam)
    {
        if (attackerTeam == BattleTeam.Ally)
        {
            return GetNextDirector(
                CinematicSequenceType.PlayerAttack,
                _playerAttackDirectors);
        }

        return GetNextDirector(
            CinematicSequenceType.MonsterAttack,
            _monsterAttackDirectors);
    }

    private PlayableDirector GetDefenderHitDirector(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _monsterHitDirector
            : _playerHitDirector;
    }


    private Vector3 GetBattleCenter()
    {
        if (_attackActor == null || _defendActor == null)
        {
            return Vector3.zero;
        }

        return (_attackActor.transform.position + _defendActor.transform.position) * 0.5f
            + Vector3.up * 1.0f;
    }

    private void LookAtTargetFlat(ActorBase actor, ActorBase target)
    {
        if (actor == null || target == null)
        {
            return;
        }

        Vector3 direction = target.transform.position - actor.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        actor.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void ClearCallbacks()
    {
        _attackActor?.ResumeAnimator();

        _onImpact = null;
        _onTurnEnd = null;
        _isAttackImpactProcessed = false;
    }
    #endregion
}
