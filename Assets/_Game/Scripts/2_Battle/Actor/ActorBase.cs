using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ActorBase : MonoBehaviour
{
    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int StunnedHash = Animator.StringToHash("Stuned");
    private static readonly int DeadHash = Animator.StringToHash("Die");

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private GameObject _auraParticleGob;

    [SerializeField]
    private ActorDissolveEffect _dissolveEffect;

    [SerializeField]
    private ParryRimEffect _parryRimEffect;

    [SerializeField]
    private int _dissolveDelayMs = 3000;

    protected int _maxHp;
    protected int _currentHp;
    private int _attackPower;

    private readonly HashSet<ActorStatusType> _statusSet = new();

    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public int AttackPower => _attackPower;

    public Animator GetAnimator => _animator;

    public bool IsDead => _currentHp <= 0;

    public virtual void Initialize(ActorTableData data)
    {
        _maxHp = data.MaxHp;
        _currentHp = _maxHp;
        _attackPower = data.Attack;

        _statusSet.Clear();

#if UNITY_EDITOR
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogError(
                    $"[{name}] Animator 컴포넌트를 찾을 수 없습니다. " +
                    "현재 오브젝트 또는 자식 오브젝트에 Animator가 있는지 확인해 주세요.",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    $"[{name}] Animator가 인스펙터에 할당되지 않아 자동으로 탐색했습니다. " +
                    "가능하면 인스펙터에서 미리 할당해 주세요.",
                    this);
            }
        }

        if (_dissolveEffect == null)
        {
            TryGetComponent<ActorDissolveEffect>(out _dissolveEffect);
        }

        if (_parryRimEffect == null)
        {
            TryGetComponent<ParryRimEffect>(out _parryRimEffect);
        }
#endif

        ActiveAuraParticle(true);
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        ActiveAuraParticle(false);

        _currentHp -= damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
            return;
        }
    }

    public void AddStatus(ActorStatusType statusType)
    {
        if (statusType == ActorStatusType.None)
        {
            return;
        }

        _statusSet.Add(statusType);
    }

    public void RemoveStatus(ActorStatusType statusType)
    {
        if (statusType == ActorStatusType.None)
        {
            return;
        }

        _statusSet.Remove(statusType);
    }

    public bool HasStatus(ActorStatusType statusType)
    {
        if (statusType == ActorStatusType.None)
        {
            return false;
        }

        return _statusSet.Contains(statusType);
    }

    protected virtual void Die()
    {
        GetAnimator.CrossFade(DeadHash, 0.1f);

        PlayDissolveDelayedAsync().Forget();
    }

    private async UniTask PlayDissolveDelayedAsync()
    {
        await UniTask.Delay(
            _dissolveDelayMs,
            ignoreTimeScale: true,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        if (_dissolveEffect == null)
        {
            return;
        }

        _dissolveEffect.PlayDissolve();
    }

    public virtual void Stunned()
    {
        GetAnimator.CrossFade(StunnedHash, 0.1f);
    }

    public void SetBlocking(bool isBlocking)
    {
        _animator.SetBool(IsBlockingHash, isBlocking);
    }

    public void PlayRimEffect()
    {
        _parryRimEffect?.Play();
    }

    public void ForceIdle()
    {
        SetBlocking(false);

        _animator.speed = 1f;
        _animator.Play(IdleHash, 0, 0f);
        _animator.Update(0f);
    }

    public void PauseAnimator()
    {
        Animator animator = GetAnimator;

        if (animator == null)
        {
            return;
        }

        animator.speed = 0f;

        //스톱시 파티클 멈춤 or 비활성화
        ActiveAuraParticle(false);
    }

    public void ResumeAnimator()
    {
        Animator animator = GetAnimator;

        if (animator == null)
        {
            return;
        }

        animator.speed = 1f;

        ActiveAuraParticle(true);
    }

    public void ActiveAuraParticle(bool active)
    {
        if (_auraParticleGob == null)
        {
            return;
        }

        _auraParticleGob.gameObject?.SetActive(active);
    }
}
