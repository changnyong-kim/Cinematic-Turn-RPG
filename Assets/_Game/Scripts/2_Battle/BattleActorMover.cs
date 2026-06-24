using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public sealed class BattleActorMover
{
    public async UniTask MoveToTargetAsync(
    Animator animator,
    Transform attacker,
    Transform target,
    float distance,
    float duration)
    {
        Vector3 direction = (target.position - attacker.position).normalized;
        Vector3 attackPosition = target.position - direction * distance;

        attacker.LookAt(target);

        animator.CrossFade("Run_Front", 0.1f);

        await attacker.DOMove(attackPosition, duration)
            .SetEase(Ease.InSine)
            .AsyncWaitForCompletion();

        animator.CrossFade("Idle", 0.05f);
    }

    public async UniTask ReturnAsync(Animator animator, Transform actor, Vector3 originPosition, float duration)
    {
        animator.CrossFade("Run_Back", 0.1f);

        await actor.DOMove(originPosition, duration)
            .SetEase(Ease.OutQuad);

        animator.CrossFade("Idle", 0.1f);
    }
}