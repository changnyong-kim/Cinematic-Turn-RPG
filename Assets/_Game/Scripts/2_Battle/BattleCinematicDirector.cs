using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public sealed class BattleCinematicDirector : MonoBehaviour
{
    private readonly BattleActorMover _actorMover = new BattleActorMover();

    [Header("Timeline")]
    [SerializeField]
    private PlayableDirector _playerAttackDirector;

    [SerializeField]
    private PlayableDirector _monsterAttackDirector;

    [SerializeField]
    private PlayableDirector _playerHitDirector;

    [SerializeField]
    private PlayableDirector _monsterHitDirector;

    [Header("Track Names")]
    [SerializeField]
    private string _playerAttackTrackName = "PlayerAnimationTrack";

    [SerializeField]
    private string _monsterAttackTrackName = "MonsterAnimationTrack";

    [SerializeField]
    private string _hitTrackName = "AnimationTrack";

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

    public void BindActors(ActorBase player, ActorBase monster)
    {
        BindAnimator(_playerAttackDirector, _playerAttackTrackName, player);
        BindAnimator(_monsterAttackDirector, _monsterAttackTrackName, monster);
        BindAnimator(_playerHitDirector, _hitTrackName, player);
        BindAnimator(_monsterHitDirector, _hitTrackName, monster);
    }

    public void PlayPlayerAttackAsync(
    ActorBase player,
    ActorBase monster,
    Func<BattleResult> onImpact,
    Func<BattleState> onTurnEnd)
    {
         PlayAttackAsync(
            attacker: player,
            defender: monster,
            attackDirector: _playerAttackDirector,
            hitDirector: _monsterHitDirector,
            approachDistance: _playerApproachDistance,
            moveDuration: _playerMoveDuration,
            returnDuration: _playerReturnDuration,
            onImpact: onImpact,
            onTurnEnd: onTurnEnd).Forget();
    }

    public void PlayMonsterAttackAsync(
        ActorBase monster,
        ActorBase player,
        Func<BattleResult> onImpact,
        Func<BattleState> onTurnEnd)
    {
        PlayAttackAsync(
            attacker: monster,
            defender: player,
            attackDirector: _monsterAttackDirector,
            hitDirector: _playerHitDirector,
            approachDistance: _monsterApproachDistance,
            moveDuration: _monsterMoveDuration,
            returnDuration: _monsterReturnDuration,
            onImpact: onImpact,
            onTurnEnd: onTurnEnd).Forget();
    }

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

        PlayDirectorAsync(attackDirector);
    }

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

        PlayDirectorAsync(_lastResult.State == BattleState.PlayerTurn ? _playerHitDirector : _monsterHitDirector);

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
        await _actorMover.ReturnAsync(
            _attackActor.GetAnimator,
            _attackActor.transform,
            _attackerOriginPosition,
            _lastResult.State == BattleState.PlayerTurn ? _playerReturnDuration : _monsterReturnDuration);

        if (_onTurnEnd != null)
        {
            _onTurnEnd();
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

    private void PlayDirectorAsync(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.time = 0;
        director.Play();
    }

    private void ClearCallbacks()
    {
        _onImpact = null;
        _onTurnEnd = null;
        _hitApplied = false;
    }
}