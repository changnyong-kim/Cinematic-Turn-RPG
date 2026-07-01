/// <summary>
/// Battle UI에 표시할 턴/결과 상태.
/// BattleModel의 전투 상태와 분리된 View 전용 상태이다.
/// </summary>
public enum BattleTurnViewState
{
    None = 0,
    PlayerTurn,
    PlayerAttack,
    MonsterAttack,
    PlayerStunned,
    MonsterStunned,
    Win,
    Lose,
}

/// <summary>
/// Battle UI가 표시할 상태를 보관하는 ViewModel.
/// BattleController는 이 ViewModel의 값만 갱신하고,
/// 실제 UI 반영은 UIBattleView의 바인딩을 통해 처리한다.
/// </summary>
public sealed class BattleViewModel
{
    public ObservableValue<(float FillAmount, string Text)> PlayerHpText { get; } = new ObservableValue<(float FillAmount, string Text)>();
    public ObservableValue<(float FillAmount, string Text)> MonsterHpText { get; } = new ObservableValue<(float FillAmount, string Text)>();

    public ObservableValue<BattleTurnViewState> TurnText { get; } = new ObservableValue<BattleTurnViewState>(BattleTurnViewState.None);
    public ObservableValue<string> SkillNotiText { get; } = new ObservableValue<string>("");

    public ObservableValue<(bool Visible, bool UseFade)> CommandUIVisible { get; } = new ObservableValue<(bool Visible, bool UseFade)>((true, true));

    public ObservableValue<(bool Visible, bool UseFade)> TurnTextVisible{ get; } = new ObservableValue<(bool Visible, bool UseFade)>((true, true));

    public ObservableValue<bool> AttackButtonInteractable { get; } = new ObservableValue<bool>(false);
    public ObservableValue<bool> ParryButtonInteractable { get; } = new ObservableValue<bool>(false);

    public void SetPlayerHp(int currentHp, int maxHp)
    {
        PlayerHpText.SetValue(CreateHpViewData(currentHp, maxHp));
    }

    public void SetMonsterHp(int currentHp, int maxHp)
    {
        MonsterHpText.SetValue(CreateHpViewData(currentHp, maxHp));
    }

    public void SetTurnText(BattleTurnViewState state)
    {
        TurnText.SetValue(state);
    }

    public void SetSkillNotiText(string text)
    {
        SkillNotiText.SetValue(text);
    }

    public void SetAttackButtonInteractable(bool isInteractable)
    {
        AttackButtonInteractable.SetValue(isInteractable);
    }

    public void SetParryButtonInteractable(bool isInteractable)
    {
        ParryButtonInteractable.SetValue(isInteractable);
    }

    public void SetCommandUIVisible(bool visible, bool useFade = true)
    {
        CommandUIVisible.SetValue((visible, useFade));
    }

    public void SetTurnUIVisible(bool visible, bool useFade = true)
    {
        TurnTextVisible.SetValue((visible, useFade));
    }

    public void HideBattleUI(bool useFade = true)
    {
        SetCommandUIVisible(false, useFade);
        SetTurnUIVisible(false, useFade);
    }

    public void ShowBattleUI(bool useFade = true)
    {
        SetCommandUIVisible(true, useFade);
        SetTurnUIVisible(true, useFade);
    }

    private static (float FillAmount, string Text) CreateHpViewData(int currentHp, int maxHp)
    {
        float fillAmount = (maxHp > 0) ? ((float)currentHp / maxHp) : 0f;

        return (fillAmount, $"{currentHp}/{maxHp}");
    }
}
