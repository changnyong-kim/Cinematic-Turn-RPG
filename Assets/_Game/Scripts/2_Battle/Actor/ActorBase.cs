using System.Collections.Generic;
using UnityEngine;

public abstract class ActorBase : MonoBehaviour
{
    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    private static readonly int StunedHash = Animator.StringToHash("Stuned");
    private static readonly int DeadHash = Animator.StringToHash("Die");


    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private GameObject _auraParticleGob;


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

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        AcitveAuraParticle(true);
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        //damage = 0;

        AcitveAuraParticle(false);

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
    }

    public virtual void Stunded()
    {
        GetAnimator.CrossFade(StunedHash, 0.1f);
    }

    public void SetBlocking(bool isBlocking)
    {
        _animator.SetBool(IsBlockingHash, isBlocking);
    }

    public void ForceIdle()
    {
        SetBlocking(false);

        _animator.speed = 1f;
        _animator.Play(IdleHash, 0, 0f);
        _animator.Update(0f);
    }

    private void SetTrigger(int triggerHash)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetTrigger(triggerHash);
    }

    public void PauseAnimator()
    {
        Animator animator = GetAnimator;

        if (animator == null)
        {
            return;
        }

        animator.speed = 0f;

        //½ºÅé½Ã ÆÄÆ¼Å¬ ¸ØÃã or ºñÈ°¼ºÈ­
        AcitveAuraParticle(false);
    }

    public void ResumeAnimator()
    {
        Animator animator = GetAnimator;

        if (animator == null)
        {
            return;
        }

        animator.speed = 1f;

        AcitveAuraParticle(true);
    }

    public void AcitveAuraParticle(bool active)
    {
        if (_auraParticleGob == null)
        {
            return;
        }

        _auraParticleGob.gameObject?.SetActive(active);
    }
}