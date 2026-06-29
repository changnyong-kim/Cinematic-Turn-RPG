using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

public sealed class BattleCinematicDirector : MonoBehaviour
{
    private readonly BattleActorMover _actorMover = new BattleActorMover();

    private IBattleCinematicEventHandler _eventHandler;

    public void BindEventHandler(IBattleCinematicEventHandler eventHandler)
    {
        _eventHandler = eventHandler;
    }

    private UniTaskCompletionSource _battleStartCompletionSource;

    /// <summary>
    /// 현재 피격 '당하는' 대상
    /// </summary>
    private PlayableDirector _currentHitDirector;
    private float _currentReturnDuration;

    [Header("Timeline")]
    [SerializeField]
    private PlayableDirector _battleStartDirector;
    [SerializeField]
    private PlayableDirector _playerAttackDirector;

    [SerializeField]
    private PlayableDirector _monsterAttackDirector;

    [SerializeField]
    private PlayableDirector _playerHitDirector;

    [SerializeField]
    private PlayableDirector _monsterHitDirector;

    [SerializeField]
    private PlayableDirector _playerBlockImpactDirector;

    [Header("Track Names")]
    [SerializeField]
    private string _playerAttackTrackName = "PlayerAnimationTrack";

    [SerializeField]
    private string _monsterAttackTrackName = "MonsterAnimationTrack";

    [SerializeField]
    private string _commonTrackName = "AnimationTrack";

    [Header("Camera Rig")]
    [SerializeField]
    private BattleCameraRig _cameraRig;

    [Header("Move")]
    [SerializeField]
    private float _playerApproachDistance = 2.5f;

    [SerializeField]
    private float _playerMoveDuration = 1.5f;

    [SerializeField]
    private float _playerReturnDuration = 1.5f;

    [SerializeField]
    private float _monsterApproachDistance = 1.5f;

    [SerializeField]
    private float _monsterMoveDuration = 1f;

    [SerializeField]
    private float _monsterReturnDuration = .8f;

    private Func<BattleResult> _onImpact;
    private Func<BattleState> _onTurnEnd;
    private BattleResult _lastResult = BattleResult.None;

    private bool _attackImpactHandled;

    private BattleTeam _currentAttackerTeam;
    private ActorBase _attackActor, _defendActor;
    private Vector3 _attackerOriginPosition;
    private Quaternion _attackerOriginRotation;

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

    public void BindActors(ActorBase player, ActorBase monster)
    {
        BindAnimator(_playerAttackDirector, _playerAttackTrackName, player);
        BindAnimator(_monsterAttackDirector, _monsterAttackTrackName, monster);
        BindAnimator(_playerHitDirector, _commonTrackName, player);
        BindAnimator(_monsterHitDirector, _commonTrackName, monster);
        BindAnimator(_playerBlockImpactDirector, _commonTrackName, player);
    }

    #region 배틀 시작 연출
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

    public void OnBattleStartEndSignal()
    {
        _battleStartCompletionSource?.TrySetResult();
        _battleStartCompletionSource = null;
    }
    #endregion

    private void PlayDirector(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.time = 0;
        director.Play();
    }

    public void PlayAttack(
    BattleTeam attackerTeam,
    ActorBase attacker,
    ActorBase defender,
    Action onApproachEnd,
    Func<BattleResult> onImpact,
    Func<BattleState> onTurnEnd)
    {
        PlayableDirector attackDirector = GetAttackDirector(attackerTeam);
        PlayableDirector hitDirector = GetDefenderHitDirector(attackerTeam);
        float approachDistance = GetApproachDistance(attackerTeam);
        float moveDuration = GetMoveDuration(attackerTeam);
        float returnDuration = GetReturnDuration(attackerTeam);

        _currentAttackerTeam = attackerTeam;
        _currentHitDirector = hitDirector;
        _currentReturnDuration = returnDuration;

        PlayAttackAsync(
            attacker,
            defender,
            attackDirector,
            hitDirector,
            approachDistance,
            moveDuration,
            returnDuration,
            onApproachEnd,
            onImpact,
            onTurnEnd).Forget();
    }

    #region Get 유틸리티
    private PlayableDirector GetAttackDirector(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _playerAttackDirector
            : _monsterAttackDirector;
    }

    private PlayableDirector GetDefenderHitDirector(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _monsterHitDirector
            : _playerHitDirector;
    }

    private PlayableDirector GetTeamHitDirector(BattleTeam targetTeam)
    {
        return targetTeam == BattleTeam.Ally
            ? _playerHitDirector
            : _monsterHitDirector;
    }

    private float GetApproachDistance(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _playerApproachDistance
            : _monsterApproachDistance;
    }

    private float GetMoveDuration(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _playerMoveDuration
            : _monsterMoveDuration;
    }

    private float GetReturnDuration(BattleTeam attackerTeam)
    {
        return attackerTeam == BattleTeam.Ally
            ? _playerReturnDuration
            : _monsterReturnDuration;
    }
    #endregion

    private async UniTask PlayAttackAsync(
        ActorBase attacker,
        ActorBase defender,
        PlayableDirector attackDirector,
        PlayableDirector hitDirector,
        float approachDistance,
        float moveDuration,
        float returnDuration,
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

        _attackImpactHandled = false;

        _cameraRig?.PlayMoveCamera(_currentAttackerTeam, attacker, defender);

        await _actorMover.MoveToTargetAsync(
            attacker.GetAnimator,
            attacker.transform,
            defender.transform,
            approachDistance,
            moveDuration);

        onApproachEnd?.Invoke();

        _cameraRig?.PlayAttackCamera(_currentAttackerTeam, attacker, defender);

        PlayDirector(attackDirector);
    }


    #region 일반 공격 시그널
    /// <summary>
    /// Timeline SignalReceiver에서 호출한다.
    /// 공격 판정 타이밍에 BattleModel 데미지 적용 + UI 갱신 콜백을 실행한다.
    /// </summary>
    public void OnAttackImpactSignal()
    {
        if (_attackImpactHandled)
        {
            return;
        }
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
                PlayDirector(_playerBlockImpactDirector);
                break;
            }
            default:
            {
                break;
            }
        }

        _attackImpactHandled = true;
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

    /// <summary>
    /// 패링 성공 시 몬스터 공격 타임라인은 종료되지만,
    //  반격 시퀀스가 아직 진행 중이므로 복귀/턴 종료는 하지 않는다.
    //  몬스터는 반격 피격 전까지 현재 자세로 경직시킨다.
    /// </summary>
    /// <returns></returns>
    public async UniTask ParryHitReactionAsync()
    {
        if (_attackActor == null)
        {
            return;
        }

        _cameraShake?.Shake(_parryShakeDuration, _parryShakeStrength);
        _cameraShake?.ZoomPunch(
            _parryZoomAmount,
            _parryZoomInDuration,
            _parryZoomOutDuration);

        _cameraShake?.MovePunchToTarget(
                GetBattleCenter(),
                0.25f,
                0.04f,
                0.12f);

        PlayableDirector attackDirector = GetAttackDirector(_currentAttackerTeam);
        attackDirector.Stop();

        await HitStopAnim(100);
    }

    public async UniTask AttackEnd()
    {
        await _actorMover.ReturnAsync(
            _attackActor.GetAnimator,
            _attackActor.transform,
            _attackerOriginPosition,
            _currentReturnDuration);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);
        
        _cameraRig?.ReturnToBase();

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }
    #endregion


    #region 패링 판정 시그널
    // 1. 패링 가능 구간 시작
    public void OnParryEnableSignal()
    {
        if (_eventHandler == null)
        {
            return;
        }

        _eventHandler.OnParryWindowOpened();
    }


    // 2. 패링 가능 구간 종료
    public void OnParryDisableSignal()
    {
        if (_eventHandler == null)
        {
            return;
        }

        _eventHandler.OnParryWindowClosed();
    }
    #endregion


    #region 반격 시그널
    public async UniTask HitStopAnim(int stopTimeMs)
    {
        _attackActor.ResumeAnimator();

        _attackActor.GetAnimator.speed = 1f;
        _attackActor.GetAnimator.CrossFade("Hit", 0.03f, 0, 0f);

        await UniTask.Delay(stopTimeMs, ignoreTimeScale: true);

        _attackActor.PauseAnimator();
    }

    public void OnParryImpactEffectSignal()
    {
        //적 경직 풀기
        //_attackActor.ResumeAnimator();

        HitStopAnim(200).Forget();

        PlayHitStopAsync(
        _parryHitStopTimeScale,
        _parryHitStopDurationMs).Forget();

        _cameraShake?.MovePunchToTarget(
            GetBattleCenter(),
            0.45f,
            0.035f,
            0.14f);

        _cameraShake?.Shake(_counterShakeDuration, _counterShakeStrength);
        _cameraShake?.ZoomPunch(
            _counterZoomAmount,
            _counterZoomInDuration,
            _counterZoomOutDuration);
    }

    public void OnParryEndSignal()
    {
        _attackActor.ResumeAnimator();
        _defendActor.ForceIdle();

        _eventHandler.OnParryEnd();
    }

    public async UniTask OnParryEndAsync()
    {
        await _actorMover.ReturnAsync(
           _attackActor.GetAnimator,
           _attackActor.transform,
           _attackerOriginPosition,
           _currentReturnDuration);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);
        _cameraRig?.ReturnToBase();

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }

    [SerializeField]
    private float _parryHitStopTimeScale = 0.15f;

    [SerializeField]
    private int _parryHitStopDurationMs = 100;

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


    #region 카메라 쉐이크 연출
    private Vector3 GetBattleCenter()
    {
        if (_attackActor == null || _defendActor == null)
        {
            return Vector3.zero;
        }

        return (_attackActor.transform.position + _defendActor.transform.position) * 0.5f
            + Vector3.up * 1.0f;
    }

    [Header("Camera Shake")]
    [SerializeField]
    private SimpleCameraShake _cameraShake;

    [SerializeField]
    private float _parryShakeDuration = 0.12f;

    [SerializeField]
    private float _parryShakeStrength = 0.07f;

    [SerializeField]
    private float _counterShakeDuration = 0.1f;

    [SerializeField]
    private float _counterShakeStrength = 0.12f;

    [Header("Camera Zoom")]
    [SerializeField]
    private float _parryZoomAmount = 5f;

    [SerializeField]
    private float _parryZoomInDuration = 0.04f;

    [SerializeField]
    private float _parryZoomOutDuration = 0.12f;

    [SerializeField]
    private float _counterZoomAmount = 7f;

    [SerializeField]
    private float _counterZoomInDuration = 0.035f;

    [SerializeField]
    private float _counterZoomOutDuration = 0.14f;
    #endregion


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
        _attackImpactHandled = false;
    }

    #region 타임라인 연출 테슽트용
    [Header("Editor Test")]
    [SerializeField]
    private PlayableDirector _testDirector;

    [ContextMenu("Test Play Director")]
    private void TestPlayDirector()
    {
        PlayDirector(_testDirector);
    }
    #endregion
}