public sealed class BattleViewModel
{
    public ObservableValue<string> PlayerHpText { get; } = new ObservableValue<string>("Player HP: -");
    public ObservableValue<string> MonsterHpText { get; } = new ObservableValue<string>("Monster HP: -");
    public ObservableValue<string> TurnText { get; } = new ObservableValue<string>("Ready");
    public ObservableValue<bool> AttackButtonInteractable { get; } = new ObservableValue<bool>(false);

    public ObservableValue<bool> ParryButtonInteractable { get; } = new ObservableValue<bool>(false);

    public void SetPlayerHp(int currentHp, int maxHp)
    {
        PlayerHpText.SetValue($"Player HP: {currentHp}/{maxHp}");
    }

    public void SetMonsterHp(int currentHp, int maxHp)
    {
        MonsterHpText.SetValue($"Monster HP: {currentHp}/{maxHp}");
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
}
