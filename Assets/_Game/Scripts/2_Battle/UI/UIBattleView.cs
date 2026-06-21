using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBattleView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _playerHpText;

    [SerializeField]
    private TextMeshProUGUI _monsterHpText;

    [SerializeField]
    private TextMeshProUGUI _turnText;

    [SerializeField]
    private Button _attackButton;

    private BattleViewModel _viewModel;

    public event Action OnAttackClicked;

    public void Bind(BattleViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;

        _viewModel.PlayerHpText.OnValueChanged += SetPlayerHpText;
        _viewModel.MonsterHpText.OnValueChanged += SetMonsterHpText;
        _viewModel.TurnText.OnValueChanged += SetTurnText;
        _viewModel.AttackButtonInteractable.OnValueChanged += SetAttackButtonInteractable;

        SetPlayerHpText(_viewModel.PlayerHpText.Value);
        SetMonsterHpText(_viewModel.MonsterHpText.Value);
        SetTurnText(_viewModel.TurnText.Value);
        SetAttackButtonInteractable(_viewModel.AttackButtonInteractable.Value);

        if (_attackButton != null)
        {
            _attackButton.onClick.AddListener(HandleAttackButtonClicked);
        }
    }

    public void Unbind()
    {
        if (_viewModel != null)
        {
            _viewModel.PlayerHpText.OnValueChanged -= SetPlayerHpText;
            _viewModel.MonsterHpText.OnValueChanged -= SetMonsterHpText;
            _viewModel.TurnText.OnValueChanged -= SetTurnText;
            _viewModel.AttackButtonInteractable.OnValueChanged -= SetAttackButtonInteractable;
            _viewModel = null;
        }

        if (_attackButton != null)
        {
            _attackButton.onClick.RemoveListener(HandleAttackButtonClicked);
        }
    }

    private void SetPlayerHpText(string text)
    {
        if (_playerHpText != null)
        {
            _playerHpText.text = text;
        }
    }

    private void SetMonsterHpText(string text)
    {
        if (_monsterHpText != null)
        {
            _monsterHpText.text = text;
        }
    }

    private void SetTurnText(string text)
    {
        if (_turnText != null)
        {
            _turnText.text = text;
        }
    }

    private void SetAttackButtonInteractable(bool isInteractable)
    {
        if (_attackButton != null)
        {
            _attackButton.interactable = isInteractable;
        }
    }

    private void HandleAttackButtonClicked()
    {
        OnAttackClicked?.Invoke();
    }
}
