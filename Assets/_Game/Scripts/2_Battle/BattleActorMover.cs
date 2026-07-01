using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public sealed class BattleActorMover : MonoBehaviour
{
    [Header("Player Move")]
    [SerializeField]
    private float _playerApproachDistance = 2.2f;

    [SerializeField]
    private float _playerMoveDuration = 1.2f;

    [SerializeField]
    private float _playerReturnDuration = 1f;

    [Header("Monster Move")]
    [SerializeField]
    private float _monsterApproachDistance = 2.5f;

    [SerializeField]
    private float _monsterMoveDuration = 0.9f;

    [SerializeField]
    private float _monsterReturnDuration = 0.9f;

    public UniTask MoveToTargetAsync(
        BattleTeam attackerTeam,
        Animator animator,
        Transform attacker,
        Transform target)
    {
        float approachDistance = GetApproachDistance(attackerTeam);
        float moveDuration = GetMoveDuration(attackerTeam);

        return MoveToTargetAsync(
            animator,
            attacker,
            target,
            approachDistance,
            moveDuration);
    }

    public UniTask ReturnAsync(
        BattleTeam attackerTeam,
        Animator animator,
        Transform actor,
        Vector3 originPosition)
    {
        float returnDuration = GetReturnDuration(attackerTeam);

        return ReturnAsync(
            animator,
            actor,
            originPosition,
            returnDuration);
    }

    private async UniTask MoveToTargetAsync(
        Animator animator,
        Transform attacker,
        Transform target,
        float distance,
        float duration)
    {
        if (animator == null || attacker == null || target == null)
        {
            return;
        }

        Vector3 direction = (target.position - attacker.position).normalized;
        Vector3 attackPosition = target.position - direction * distance;

        attacker.LookAt(target);

        animator.CrossFade("Run_Front", 0.1f);

        await attacker.DOMove(attackPosition, duration)
            .SetEase(Ease.InSine)
            .AsyncWaitForCompletion();

        animator.CrossFade("Idle", 0.05f);
    }

    private async UniTask ReturnAsync(
        Animator animator,
        Transform actor,
        Vector3 originPosition,
        float duration)
    {
        if (animator == null || actor == null)
        {
            return;
        }

        animator.CrossFade("Run_Back", 0.1f);

        await actor.DOMove(originPosition, duration)
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion();

        animator.CrossFade("Idle", 0.1f);
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
}
