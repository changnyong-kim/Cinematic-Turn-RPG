using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBattleView : MonoBehaviour
{
    /// <summary>
    /// 메인 ViewModel
    /// </summary>
    private BattleViewModel _viewModel;
 
    /// <summary>
    /// 클릭 이벤트
    /// </summary>
    public event Action OnAttackClicked, OnParryClicked;

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

    [Header("Canvas Group")]
    [SerializeField]
    private CanvasGroup _commandUICanvasGroup;

    [SerializeField]
    private CanvasGroup _turnUICanvasGroup;

    [Header("Fade")]
    [SerializeField]
    private float _commandUIFadeDuration = 0.2f;

    [SerializeField]
    private float _turnUIFadeDuration = 0.15f;

    private Tween _commandUITween;
    private Tween _turnUITween;

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

        _viewModel.CommandUIVisible.OnValueChanged += SetCommandUIVisible;
        _viewModel.TurnTextVisible.OnValueChanged += SetTurnUIVisible;

        SetCommandUIVisible(_viewModel.CommandUIVisible.Value);
        SetTurnUIVisible(_viewModel.TurnTextVisible.Value);
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

    /*
    private void SetCommandUIVisible(bool visible)
    {
        if (_turnUICanvasGroup != null)
        {
            SetCanvasGroupVisible(_turnUICanvasGroup, visible);
        }
    }

    private void SetTurnUIVisible(bool visible)
    {
        if (_turnUICanvasGroup != null)
        {
            SetCanvasGroupVisible(_turnUICanvasGroup, visible);
        }
    }

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
    */

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

    private void SetCommandUIVisible((bool visible, bool useFade) uiState)
    {
        _commandUITween?.Kill();

        if(uiState.useFade == false)
        {
            _commandUICanvasGroup.gameObject.SetActive(uiState.visible);
            return;
        }

        _commandUICanvasGroup.gameObject.SetActive(uiState.visible);

        _commandUITween = uiState.visible
            ? CanvasGroupTweenUtility.FadeIn(
                _commandUICanvasGroup,
                _commandUIFadeDuration,
                Ease.OutQuad,
                true)
            : CanvasGroupTweenUtility.FadeOut(
                _commandUICanvasGroup,
                _commandUIFadeDuration,
                Ease.InQuad,
                true,
                false);
    }

    private void SetTurnUIVisible((bool visible, bool useFade) uiState)
    {
        _turnUITween?.Kill();

        if (uiState.useFade == false)
        {
            _turnUICanvasGroup.gameObject.SetActive(uiState.visible);
            return;
        }

        _turnUICanvasGroup.gameObject.SetActive(uiState.visible);

        _turnUITween = uiState.visible
            ? CanvasGroupTweenUtility.FadeIn(
                _turnUICanvasGroup,
                _turnUIFadeDuration,
                Ease.OutQuad,
                false)
            : CanvasGroupTweenUtility.FadeOut(
                _turnUICanvasGroup,
                _turnUIFadeDuration,
                Ease.InQuad,
                true,
                false);
    }

    private void OnDestroy()
    {
        _commandUITween?.Kill();
        _turnUITween?.Kill();

        Unbind();
    }

}