using UnityEngine;

/// <summary>
/// 전투 행동 처리 결과.
/// 상태 전이 결과와 방어자 반응 타입을 함께 전달하여
/// 전투 로직과 연출 분기를 분리한다.
/// </summary>
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

    public bool IsStatusSkipped
    {
        get;
    }

    public ActorStatusType SkippedStatusType
    {
        get;
    }

    public bool IsFinished => State == BattleState.Win || State == BattleState.Lose;

    public BattleResult(
        BattleState state,
        DefenderReactionType reactionType = DefenderReactionType.Hit,
        bool isStatusSkipped = false,
        ActorStatusType skippedStatusType = ActorStatusType.None)
    {
        State = state;
        ReactionType = reactionType;
        IsStatusSkipped = isStatusSkipped;
        SkippedStatusType = skippedStatusType;
    }
}

/// <summary>
/// 전투 규칙과 상태 전이를 담당하는 Model.
/// 데미지, 턴 전환, 패링/스턴 판정만 처리하고
/// UI와 Timeline 연출에는 직접 의존하지 않는다.
/// </summary>
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
        Monster.IsDead == false &&
        Player.HasStatus(ActorStatusType.Stun) == false;

    public bool CanMonsterAct =>
        State == BattleState.MonsterTurn &&
        Player != null &&
        Monster != null &&
        Player.IsDead == false &&
        Monster.IsDead == false &&
        Monster.HasStatus(ActorStatusType.Stun) == false;


    #region 패링 필드
    private bool _isParryWindowOpen;
    private bool _isParryRequested;

    public bool CanRequestParry =>
        State == BattleState.MonsterTurn &&
        _isParryWindowOpen &&
        _isParryRequested == false &&
        Player != null &&
        Monster != null &&
        Player.IsDead == false &&
        Monster.IsDead == false;
    #endregion


    public BattleModel(ActorBase player, ActorBase monster)
    {
        Player = player;
        Monster = monster;
        State = BattleState.PlayerTurn;
    }

    /// <summary>
    /// 턴 시작 시 상태 이상으로 인해 행동이 스킵되는지 판정한다.
    /// 현재는 Stun만 처리한다.
    /// </summary>
    public BattleResult ResolveTurnStart()
    {
        switch (State)
        {
            case BattleState.PlayerTurn:
            {
                return ResolveActorTurnStart(Player, BattleState.MonsterTurn);
            }

            case BattleState.MonsterTurn:
            {
                return ResolveActorTurnStart(Monster, BattleState.PlayerTurn);
            }

            default:
            {
                return new BattleResult(State);
            }
        }
    }

    private BattleResult ResolveActorTurnStart(ActorBase actor, BattleState nextState)
    {
        if (actor == null || actor.IsDead)
        {
            return new BattleResult(State);
        }

        if (actor.HasStatus(ActorStatusType.Stun))
        {
            actor.RemoveStatus(ActorStatusType.Stun);
            State = nextState;

            return new BattleResult(
                State,
                isStatusSkipped: true,
                skippedStatusType: ActorStatusType.Stun);
        }

        return new BattleResult(State);
    }


    #region 스킬데이터 처리
    public BattleResult UsePlayerSkill(BattleSkillTableData skillData)
    {
        if (CanPlayerAct == false)
        {
            return new BattleResult(State);
        }

        if (skillData == null)
        {
            return new BattleResult(State);
        }

        ApplySkillDamage(Player, Monster, skillData);
        ApplySkillStatus(Monster, skillData);

        if (Monster.IsDead)
        {
            State = BattleState.Win;
            ClearParryWindow();

            return new BattleResult(State);
        }

        // 패리 성공 상태면 몬스터 턴을 스킵하고 플레이어 턴으로 복귀한다.
        State = (_isParryRequested) ? BattleState.PlayerTurn : BattleState.MonsterTurn;

        ClearParryWindow();

        return new BattleResult(State);
    }

    public BattleResult UseMonsterSkill(BattleSkillTableData skillData)
    {
        if (CanMonsterAct == false)
        {
            return new BattleResult(State);
        }

        if (skillData == null)
        {
            return new BattleResult(State);
        }

        if (skillData.CanParry && _isParryRequested)
        {
            State = BattleState.PlayerTurn;
            CloseParryWindow();
            return new BattleResult(State, DefenderReactionType.Parry);
        }

        ApplySkillDamage(Monster, Player, skillData);
        ApplySkillStatus(Player, skillData);

        if (Player.IsDead)
        {
            State = BattleState.Lose;
            ClearParryWindow();

            return new BattleResult(State);
        }

        State = BattleState.PlayerTurn;

        return new BattleResult(State);
    }

    private void ApplySkillDamage(ActorBase attacker, ActorBase defender, BattleSkillTableData skillData)
    {
        if (attacker == null || defender == null || skillData == null)
        {
            return;
        }

        int damage = Mathf.RoundToInt(attacker.AttackPower * skillData.PowerRate);
        defender.TakeDamage(damage);
    }

    private void ApplySkillStatus(ActorBase target, BattleSkillTableData skillData)
    {
        if (target == null || skillData == null)
        {
            return;
        }

        if (skillData.ApplyStatus == ActorStatusType.None)
        {
            return;
        }

        target.AddStatus(skillData.ApplyStatus);
    }
    #endregion


    #region 패링
    public void OpenParryWindow()
    {
        if (State != BattleState.MonsterTurn)
        {
            return;
        }

        if (Player == null || Monster == null)
        {
            return;
        }

        if (Player.IsDead || Monster.IsDead)
        {
            return;
        }

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

    private void ClearParryWindow()
    {
        _isParryWindowOpen = false;
        _isParryRequested = false;
    }
    #endregion
}
