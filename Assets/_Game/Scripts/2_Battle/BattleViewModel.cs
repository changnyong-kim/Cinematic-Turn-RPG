public sealed class BattleViewModel
{
    public ObservableValue<(float, string)> PlayerHpText { get; } = new ObservableValue<(float, string)>();
    public ObservableValue<(float, string)> MonsterHpText { get; } = new ObservableValue<(float, string)>();
    public ObservableValue<string> TurnText { get; } = new ObservableValue<string>("Ready");

    public ObservableValue<bool> CommandUIVisible { get; } = new ObservableValue<bool>(true);
    public ObservableValue<bool> TurnTextVisible { get; } = new ObservableValue<bool>(true);


    public ObservableValue<bool> AttackButtonInteractable { get; } = new ObservableValue<bool>(false);

    public ObservableValue<bool> ParryButtonInteractable { get; } = new ObservableValue<bool>(false);

    public void SetPlayerHp(int currentHp, int maxHp)
    {
        var fillAmount = (float)currentHp / maxHp;
        PlayerHpText.SetValue((fillAmount, $"{currentHp}/{maxHp}"));
    }

    public void SetMonsterHp(int currentHp, int maxHp)
    {
        var fillAmount = (float)currentHp / maxHp;
        MonsterHpText.SetValue((fillAmount, $"{currentHp}/{maxHp}"));
    }

    public void SetTurnText(string text)
    {
        TurnText.SetValue(text);
    }

    public void SetAttackButtonInteractable(bool isInteractable)
    {
        AttackButtonInteractable.SetValue(isInteractable);
    }

    public void SetParryButtonInteractable(bool isInteractable)
    {
        ParryButtonInteractable.SetValue(isInteractable);
    }

    public void SetCommandUIVisible(bool visible)
    {
        CommandUIVisible.SetValue(visible);
    }

    public void SetTurnUIVisible(bool visible)
    {
        TurnTextVisible.SetValue(visible);
    }
}
