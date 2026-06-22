public readonly struct BattleResult
{
    public static BattleResult None => default;

    public BattleState State
    {
        get;
    }

    public DefenderReactionType ReactionType
    {
        get;
    }

    public bool IsFinished => State == BattleState.Win || State == BattleState.Lose;

    public BattleResult( BattleState state, DefenderReactionType reactionType = DefenderReactionType.Hit)
    {
        State = state;
        ReactionType = reactionType;
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

        if (_isParryRequested)
        {
            State = BattleState.PlayerTurn;
            _isParryRequested = false;
            _isParryWindowOpen = false;
            return new BattleResult(State, DefenderReactionType.Parry);
        }

        Monster.Attack(Player);

        if (Player.IsDead)
        {
            State = BattleState.Lose;
            return new BattleResult(State);
        }

        State = BattleState.PlayerTurn;
        _isParryWindowOpen = false;
        return new BattleResult(State);
    }


    #region ÆÐ¸µ
    private bool _isParryWindowOpen;
    private bool _isParryRequested;

    public bool CanRequestParry => State == BattleState.MonsterTurn
                                   && _isParryWindowOpen
                                   && _isParryRequested == false;

    public void OpenParryWindow()
    {
        _isParryWindowOpen = true;
        _isParryRequested = false;
    }

    public void RequestParry()
    {
        if (CanRequestParry == false)
        {
            return;
        }

        _isParryRequested = true;
    }

    public void CloseParryWindow()
    {
        _isParryWindowOpen = false;
    }
    #endregion
}
