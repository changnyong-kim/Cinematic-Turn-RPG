using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBattleView : MonoBehaviour
{
    [Header("Turn Text Materials")]
    [SerializeField]
    private Material _normalTurnMaterial;

    [SerializeField]
    private Material _playerTurnMaterial;

    [SerializeField]
    private Material _monsterTurnMaterial;

    [SerializeField]
    private TextMeshProUGUI _playerHpText;

    [SerializeField]
    private Image _playerHpImg;

    [SerializeField]
    private TextMeshProUGUI _monsterHpText;

    [SerializeField]
    private Image _monsterHpImg;

    [SerializeField]
    private TextMeshProUGUI _turnText;

    [SerializeField]
    private GameObject _playerTrunBar, _monsterTrunBar;

    [SerializeField]
    private Button _attackButton, _parryButton;

    private BattleViewModel _viewModel;

    public event Action OnAttackClicked, OnParryClicked;

    public void Bind(BattleViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;

        _viewModel.PlayerHpText.OnValueChanged += SetPlayerHpText;
        _viewModel.MonsterHpText.OnValueChanged += SetMonsterHpText;
        _viewModel.TurnText.OnValueChanged += SetTurnText;

        _viewModel.AttackButtonInteractable.OnValueChanged += SetAttackButtonInteractable;
        _viewModel.ParryButtonInteractable.OnValueChanged += SetParryButtonInteractable;

        SetPlayerHpText(_viewModel.PlayerHpText.Value);
        SetMonsterHpText(_viewModel.MonsterHpText.Value);
        SetTurnText(_viewModel.TurnText.Value);

        SetAttackButtonInteractable(_viewModel.AttackButtonInteractable.Value);
        SetParryButtonInteractable(_viewModel.ParryButtonInteractable.Value);

        if (_attackButton != null)
        {
            _attackButton.onClick.AddListener(HandleAttackButtonClicked);
        }

        if (_parryButton != null)
        {
            _parryButton.onClick.AddListener(HandleParryButtonClicked);
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
            _viewModel.ParryButtonInteractable.OnValueChanged -= SetParryButtonInteractable;
            _viewModel = null;
        }

        if (_attackButton != null)
        {
            _attackButton.onClick.RemoveListener(HandleAttackButtonClicked);
        }

        if(_parryButton != null)
        {
            _parryButton.onClick.RemoveListener(HandleParryButtonClicked);
        }
    }

    private void SetPlayerHpText((float, string) hpInfo)
    {
        if (_playerHpText != null)
        {
            _playerHpImg.fillAmount = hpInfo.Item1;
            _playerHpText.text = hpInfo.Item2;
        }
    }

    private void SetMonsterHpText((float, string) hpInfo)
    {

        if (_monsterHpText != null)
        {
            _monsterHpImg.fillAmount = hpInfo.Item1;
            _monsterHpText.text = hpInfo.Item2;
        }
    }

    private void SetTurnText(string text)
    {
        if (_turnText != null)
        {
            _turnText.text = text;
            ApplyTurnTextMaterial(text);
        }
    }

    private void ApplyTurnTextMaterial(string text)
    {
        if (_turnText == null)
        {
            return;
        }

        if (text == "Player Turn" || text == "PLAYER TURN")
        {
            _turnText.text = "PLAYER TURN";
            _turnText.fontSharedMaterial = _playerTurnMaterial;

            TurnBarSetting(true);
            
            return;
        }

        if (text == "Monster Attack" || text == "MONSTER ATTACK")
        {
            _turnText.text = "MONSTER ATTACK";
            _turnText.fontSharedMaterial = _monsterTurnMaterial;

            TurnBarSetting(false);

            return;
        }

        if (text == "Parry Success" || text == "P A R R Y")
        {
            _turnText.text = "P A R R Y";
            _turnText.fontSharedMaterial = _playerTurnMaterial;

            TurnBarSetting(true);

            return;
        }

        _turnText.fontSharedMaterial = _normalTurnMaterial;
    }

    private void TurnBarSetting(bool isPlayerState)
    {
        _playerTrunBar.SetActive(isPlayerState);
        _monsterTrunBar.SetActive(! isPlayerState);
    }

    private void SetAttackButtonInteractable(bool isInteractable)
    {
        if (_attackButton != null)
        {
            _attackButton.interactable = isInteractable;
        }
    }

    private void SetParryButtonInteractable(bool isInteractable)
    {
        if (_parryButton != null)
        {
            _parryButton.interactable = isInteractable;
        }
    }

    private void HandleAttackButtonClicked()
    {
        OnAttackClicked?.Invoke();
    }

    private void HandleParryButtonClicked()
    {
        OnParryClicked?.Invoke();
    }
}