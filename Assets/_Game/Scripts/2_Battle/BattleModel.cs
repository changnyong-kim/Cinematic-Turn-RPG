public enum BattleState
{
    None = 0,
    PlayerTurn,
    MonsterTurn,
    Win,
    Lose,
}

public readonly struct BattleResult
{
    public static BattleResult None => default;

    public BattleState State
    {
        get;
    }

    public bool IsFinished => State == BattleState.Win || State == BattleState.Lose;

    public BattleResult(BattleState state)
    {
        State = state;
    }
}

public sealed class BattleModel
{
    public ActorBase Player
    {
        get;
    }

    public ActorBase Monster
    {
        get;
    }

    public BattleState State
    {
        get; private set;
    }

    public bool CanPlayerAct =>
        State == BattleState.PlayerTurn &&
        Player != null &&
        Monster != null &&
        Player.IsDead == false &&
        Monster.IsDead == false;

    public bool CanMonsterAct =>
        State == BattleState.MonsterTurn &&
        Player != null &&
        Monster != null &&
        Player.IsDead == false &&
        Monster.IsDead == false;

    public BattleModel(ActorBase player, ActorBase monster)
    {
        Player = player;
        Monster = monster;
        State = BattleState.PlayerTurn;
    }

    public BattleResult PlayerAttack()
    {
        if (CanPlayerAct == false)
        {
            return new BattleResult(State);
        }

        Player.Attack(Monster);

        if (Monster.IsDead)
        {
            State = BattleState.Win;
            return new BattleResult(State);
        }

        State = BattleState.MonsterTurn;
        return new BattleResult(State);
    }

    public BattleResult MonsterAttack()
    {
        if (CanMonsterAct == false)
        {
            return new BattleResult(State);
        }

        Monster.Attack(Player);

        if (Player.IsDead)
        {
            State = BattleState.Lose;
            return new BattleResult(State);
        }

        State = BattleState.PlayerTurn;
        return new BattleResult(State);
    }
}
