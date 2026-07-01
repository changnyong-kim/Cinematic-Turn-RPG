public enum ActorType
{
    None = 0,
    Player = 1,
    Monster = 2,
}

public enum BattleState
{
    None = 0,
    PlayerTurn,
    MonsterTurn,
    Win,
    Lose,
}

public enum BattleTeam
{
    None = 0,
    Ally,
    Enemy,
}

/// <summary>
/// ReactionType은 항상 공격자가 아니라 방어자 기준
/// </summary>
public enum DefenderReactionType
{
    None = 0,
    Hit,
    Block,
    Parry,
}

public enum ActorStatusType
{
    None = 0,
    Stun = 1,
}

public enum BattleSkillId
{
    None = 0,
    PlayerNormalAttack = 100,
    MonsterNormalAttack = 200,
    MonsterStunAttack = 201,
}
