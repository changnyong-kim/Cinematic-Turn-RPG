using UnityEngine;

public abstract class ActorBase : MonoBehaviour
{
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");
    private static readonly int DodgeTrigger = Animator.StringToHash("Dodge");
    private static readonly int DeadTrigger = Animator.StringToHash("Dead");

    [SerializeField]
    private Animator _animator;

    protected int _maxHp;
    protected int _currentHp;
    private int _attackPower;

    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public int AttackPower => _attackPower;


    public bool IsDead => _currentHp <= 0;

    public virtual void Initialize(ActorTableData data)
    {
        _maxHp = data.MaxHp;
        _currentHp = _maxHp;
        _attackPower = data.Attack;

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        damage = 0;

        _currentHp -= damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
            return;
        }

        PlayHit();
    }

    public virtual void Attack(ActorBase target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        PlayAttack();

        target.TakeDamage(_attackPower);
    }

    public virtual void PlayAttack()
    {
        SetTrigger(AttackTrigger);
    }

    public virtual void PlayHit()
    {
        SetTrigger(HitTrigger);
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} Dead");
    }

    private void SetTrigger(int triggerHash)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetTrigger(triggerHash);
    }
}