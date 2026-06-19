using UnityEngine;

public abstract class ActorBase : MonoBehaviour
{
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
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        _currentHp -= damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    public virtual void Attack(ActorBase target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(_attackPower);
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} Dead");
    }
}