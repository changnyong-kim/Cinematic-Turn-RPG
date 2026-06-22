using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Playables;

public sealed class BattleCinematicDirector : MonoBehaviour
{
    private readonly BattleActorMover _actorMover = new BattleActorMover();

    /// <summary>
    /// 현재 피격 '당하는' 대상
    /// </summary>
    private PlayableDirector _currentHitDirector;
    private float _currentReturnDuration;

    [Header("Timeline")]
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
    private bool _hitApplied;

    ActorBase _attackActor;
    Vector3 _attackerOriginPosition;
    Quaternion _attackerOriginRotation;

    public void BindActors(ActorBase player, ActorBase monster)
    {
        BindAnimator(_playerAttackDirector, _playerAttackTrackName, player);
        BindAnimator(_monsterAttackDirector, _monsterAttackTrackName, monster);
        BindAnimator(_playerHitDirector, _commonTrackName, player);
        BindAnimator(_monsterHitDirector, _commonTrackName, monster);
        BindAnimator(_playerBlockImpactDirector, _commonTrackName, player);
    }

    public void PlayAttack(
    BattleTeam attackerTeam,
    ActorBase attacker,
    ActorBase defender,
    Func<BattleResult> onImpact,
    Func<BattleState> onTurnEnd)
    {
        PlayableDirector attackDirector = GetAttackDirector(attackerTeam);
        PlayableDirector hitDirector = GetDefenderHitDirector(attackerTeam);
        float approachDistance = GetApproachDistance(attackerTeam);
        float moveDuration = GetMoveDuration(attackerTeam);
        float returnDuration = GetReturnDuration(attackerTeam);

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

        _onImpact = onImpact;
        _onTurnEnd = onTurnEnd;

        _lastResult = BattleResult.None;

        _hitApplied = false;

        await _actorMover.MoveToTargetAsync(
            attacker.GetAnimator,
            attacker.transform,
            defender.transform,
            approachDistance,
            moveDuration);

        PlayDirector(attackDirector);
    }


    #region 일반 공격 시그널
    /// <summary>
    /// Timeline SignalReceiver에서 호출한다.
    /// 공격 판정 타이밍에 BattleModel 데미지 적용 + UI 갱신 콜백을 실행한다.
    /// </summary>
    public void OnAttackImpactSignal()
    {
        if (_hitApplied)
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
                PlayDirector(_currentHitDirector);
                break;

            case DefenderReactionType.Parry:
                PlayDirector(_playerBlockImpactDirector);
                break;
        }

        _hitApplied = true;
    }

    public void OnAttackEndSignal()
    {
        if (_lastResult.IsFinished)
        {
            ClearCallbacks();
            return;
        }

        AttackEnd().Forget();
    }

    public async UniTask AttackEnd()
    {
        if (_lastResult.ReactionType == DefenderReactionType.Parry)
        {
            _attackActor.PlayIdle();

            if (_onTurnEnd != null)
            {
                _onTurnEnd();
            }

            return;
        }

        await _actorMover.ReturnAsync(
            _attackActor.GetAnimator,
            _attackActor.transform,
            _attackerOriginPosition,
            _currentReturnDuration);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }
    #endregion


    #region 반격 시그널
    public void OnParryImpactSignal()
    {
        // TODO: counter hit VFX, monster hit reaction, camera shake

        PlayDirector(GetTeamHitDirector(BattleTeam.Enemy));
    }

    public void OnParryEndSignal()
    {
        OnParryEndAsync().Forget();
    }

    private async UniTask OnParryEndAsync()
    {
        await _actorMover.ReturnAsync(
           _attackActor.GetAnimator,
           _attackActor.transform,
           _attackerOriginPosition,
           _currentReturnDuration);

        // 최종 보정
        _attackActor.transform.SetPositionAndRotation(_attackerOriginPosition, _attackerOriginRotation);

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
        }
    }
    #endregion

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

    private void ClearCallbacks()
    {
        _onImpact = null;
        _onTurnEnd = null;
        _hitApplied = false;
    }
}