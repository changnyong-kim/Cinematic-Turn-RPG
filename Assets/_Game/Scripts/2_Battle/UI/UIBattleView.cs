using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// BattleViewModel의 상태를 실제 전투 UI에 반영하는 View.
/// 전투 로직은 처리하지 않고, ViewModel 값 변경에 따른 표시/버튼/페이드 처리만 담당한다.
/// </summary>
public sealed class UIBattleView : MonoBehaviour
{
    private readonly struct TurnTextStyle
    {
        public Material Material
        {
            get;
        }
        public bool IsPlayerTurnBar
        {
            get;
        }
        public bool PlayTurnSound
        {
            get;
        }

        public TurnTextStyle(Material material, bool isPlayerTurnBar, bool playTurnSound)
        {
            Material = material;
            IsPlayerTurnBar = isPlayerTurnBar;
            PlayTurnSound = playTurnSound;
        }
    }

    private BattleViewModel _viewModel;

    public event Action OnAttackClicked;
    public event Action OnParryClicked;

    [Header("Turn Text Materials")]
    [SerializeField]
    private Material _normalTurnMaterial;

    [SerializeField]
    private Material _playerTurnMaterial;

    [SerializeField]
    private Material _monsterTurnMaterial;

    [Header("HP")]
    [SerializeField]
    private TextMeshProUGUI _playerHpText;

    [SerializeField]
    private Image _playerHpImg;

    [SerializeField]
    private TextMeshProUGUI _monsterHpText;

    [SerializeField]
    private Image _monsterHpImg;

    [Header("Turn")]
    [SerializeField]
    private TextMeshProUGUI _turnText;

    [SerializeField]
    private TextMeshProUGUI _skillNotiText;

    [SerializeField]
    [FormerlySerializedAs("_playerTrunBar")]
    private GameObject _playerTurnBar;

    [SerializeField]
    [FormerlySerializedAs("_monsterTrunBar")]
    private GameObject _monsterTurnBar;

    [Header("Buttons")]
    [SerializeField]
    private Button _attackButton;

    [SerializeField]
    private Button _parryButton;

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

        if (viewModel == null)
        {
            return;
        }

        _viewModel = viewModel;

        _viewModel.PlayerHpText.OnValueChanged += SetPlayerHpText;
        _viewModel.MonsterHpText.OnValueChanged += SetMonsterHpText;
        _viewModel.TurnText.OnValueChanged += SetTurnText;
        _viewModel.SkillNotiText.OnValueChanged += SetSkillNotiText;
        _viewModel.AttackButtonInteractable.OnValueChanged += SetAttackButtonInteractable;
        _viewModel.ParryButtonInteractable.OnValueChanged += SetParryButtonInteractable;
        _viewModel.CommandUIVisible.OnValueChanged += SetCommandUIVisible;
        _viewModel.TurnTextVisible.OnValueChanged += SetTurnUIVisible;

        ApplyCurrentViewModelState();
        BindButtonEvents();
    }

    public void Unbind()
    {
        if (_viewModel != null)
        {
            _viewModel.PlayerHpText.OnValueChanged -= SetPlayerHpText;
            _viewModel.MonsterHpText.OnValueChanged -= SetMonsterHpText;
            _viewModel.TurnText.OnValueChanged -= SetTurnText;
            _viewModel.SkillNotiText.OnValueChanged -= SetSkillNotiText;
            _viewModel.AttackButtonInteractable.OnValueChanged -= SetAttackButtonInteractable;
            _viewModel.ParryButtonInteractable.OnValueChanged -= SetParryButtonInteractable;
            _viewModel.CommandUIVisible.OnValueChanged -= SetCommandUIVisible;
            _viewModel.TurnTextVisible.OnValueChanged -= SetTurnUIVisible;

            _viewModel = null;
        }

        UnbindButtonEvents();
    }

    private void ApplyCurrentViewModelState()
    {
        SetPlayerHpText(_viewModel.PlayerHpText.Value);
        SetMonsterHpText(_viewModel.MonsterHpText.Value);
        SetTurnText(_viewModel.TurnText.Value);
        SetSkillNotiText(_viewModel.SkillNotiText.Value);
        SetAttackButtonInteractable(_viewModel.AttackButtonInteractable.Value);
        SetParryButtonInteractable(_viewModel.ParryButtonInteractable.Value);
        SetCommandUIVisible(_viewModel.CommandUIVisible.Value);
        SetTurnUIVisible(_viewModel.TurnTextVisible.Value);
    }

    private void BindButtonEvents()
    {
        if (_attackButton != null)
        {
            _attackButton.onClick.AddListener(HandleAttackButtonClicked);
        }

        if (_parryButton != null)
        {
            _parryButton.onClick.AddListener(HandleParryButtonClicked);
        }
    }

    private void UnbindButtonEvents()
    {
        if (_attackButton != null)
        {
            _attackButton.onClick.RemoveListener(HandleAttackButtonClicked);
        }

        if (_parryButton != null)
        {
            _parryButton.onClick.RemoveListener(HandleParryButtonClicked);
        }
    }

    /// <summary>
    /// Turn 상태를 UI 표시 텍스트로 변환한다.
    /// </summary>
    private string GetTurnText(BattleTurnViewState state)
    {
        switch (state)
        {
            case BattleTurnViewState.PlayerTurn:
            {
                return "PLAYER TURN";
            }
            case BattleTurnViewState.PlayerAttack:
            {
                return "PLAYER ATTACK";
            }
            case BattleTurnViewState.MonsterAttack:
            {
                return "MONSTER ATTACK";
            }
            case BattleTurnViewState.PlayerStunned:
            {
                return "PLAYER STUNNED";
            }
            case BattleTurnViewState.MonsterStunned:
            {
                return "MONSTER STUNNED";
            }
            case BattleTurnViewState.Win:
            {
                return "WIN";
            }
            case BattleTurnViewState.Lose:
            {
                return "LOSE";
            }
            case BattleTurnViewState.None:
            default:
            {
                return string.Empty;
            }
        }
    }

    private TurnTextStyle GetTurnTextStyle(BattleTurnViewState state)
    {
        switch (state)
        {
            case BattleTurnViewState.PlayerTurn:
            {
                return new TurnTextStyle(_playerTurnMaterial, true, true);
            }
            case BattleTurnViewState.PlayerAttack:
            {
                return new TurnTextStyle(_playerTurnMaterial, true, false);
            }
            case BattleTurnViewState.MonsterAttack:
            {
                return new TurnTextStyle(_monsterTurnMaterial, false, true);
            }
            case BattleTurnViewState.PlayerStunned:
            {
                return new TurnTextStyle(_monsterTurnMaterial, false, false);
            }
            case BattleTurnViewState.MonsterStunned:
            {
                return new TurnTextStyle(_playerTurnMaterial, true, false);
            }
            case BattleTurnViewState.Win:
            {
                return new TurnTextStyle(_playerTurnMaterial, true, false);
            }
            case BattleTurnViewState.Lose:
            {
                return new TurnTextStyle(_monsterTurnMaterial, false, false);
            }
            case BattleTurnViewState.None:
            default:
            {
                return new TurnTextStyle(_normalTurnMaterial, true, false);
            }
        }
    }

    private void SetPlayerHpText((float FillAmount, string Text) hpInfo)
    {
        SetHpView(_playerHpImg, _playerHpText, hpInfo);
    }

    private void SetMonsterHpText((float FillAmount, string Text) hpInfo)
    {
        SetHpView(_monsterHpImg, _monsterHpText, hpInfo);
    }

    private void SetHpView(Image hpImage, TextMeshProUGUI hpText, (float FillAmount, string Text) hpInfo)
    {
        if (hpImage != null)
        {
            hpImage.fillAmount = hpInfo.FillAmount;
        }

        if (hpText != null)
        {
            hpText.text = hpInfo.Text;
        }
    }

    private void SetTurnText(BattleTurnViewState state)
    {
        if (_turnText != null)
        {
            _turnText.text = GetTurnText(state);

            TurnTextStyle style = GetTurnTextStyle(state);
            _turnText.fontSharedMaterial = style.Material;
            SetTurnBar(style.IsPlayerTurnBar);

            if (style.PlayTurnSound)
            {
                PlayTurnChangeSound();
            }
        }
    }

    private void SetSkillNotiText(string text)
    {
        if (_skillNotiText != null)
        {
            _skillNotiText.text = text;
        }
    }

    private void SetTurnBar(bool isPlayerState)
    {
        if (_playerTurnBar != null)
        {
            _playerTurnBar.SetActive(isPlayerState);
        }

        if (_monsterTurnBar != null)
        {
            _monsterTurnBar.SetActive(isPlayerState == false);
        }
    }

    private void PlayTurnChangeSound()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(AudioCueId.TurnChange);
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

    private void SetCommandUIVisible((bool Visible, bool UseFade) uiState)
    {
        _commandUITween = SetCanvasGroupVisible(
            _commandUICanvasGroup,
            _commandUITween,
            uiState.Visible,
            uiState.UseFade,
            _commandUIFadeDuration,
            Ease.OutQuad,
            Ease.InQuad,
            true);
    }

    private void SetTurnUIVisible((bool Visible, bool UseFade) uiState)
    {
        _turnUITween = SetCanvasGroupVisible(
            _turnUICanvasGroup,
            _turnUITween,
            uiState.Visible,
            uiState.UseFade,
            _turnUIFadeDuration,
            Ease.OutQuad,
            Ease.InQuad,
            false);
    }

    private Tween SetCanvasGroupVisible(
        CanvasGroup canvasGroup,
        Tween currentTween,
        bool visible,
        bool useFade,
        float fadeDuration,
        Ease fadeInEase,
        Ease fadeOutEase,
        bool changeInteractable)
    {
        currentTween?.Kill();

        if (canvasGroup == null)
        {
            return null;
        }

        canvasGroup.gameObject.SetActive(visible);

        if (useFade == false)
        {
            canvasGroup.alpha = visible ? 1f : 0f;

            if (changeInteractable)
            {
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            return null;
        }

        return visible
            ? CanvasGroupTweenUtility.FadeIn(
                canvasGroup,
                fadeDuration,
                fadeInEase,
                changeInteractable)
            : CanvasGroupTweenUtility.FadeOut(
                canvasGroup,
                fadeDuration,
                fadeOutEase,
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
