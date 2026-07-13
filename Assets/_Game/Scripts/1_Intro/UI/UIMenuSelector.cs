using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using System;

public class UIMenuSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private RectTransform[] _buttons;

    [SerializeField]
    private UISelectButton[] _selectButtons;

    [Header("Move FX")]
    [SerializeField] private RectTransform _moveFxTransform;
    [SerializeField] private UIParticle _moveFxParticle;

    [Header("Move Setting")]
    [SerializeField] private float _moveDuration = 0.12f;
    [SerializeField] private Ease _moveEase = Ease.OutQuad;
    [SerializeField] private bool _useJumpMove = true;
    [SerializeField] private float _jumpPower = 8f;

    [Header("Scale Punch")]
    [SerializeField] private float _punchScale = 0.12f;
    [SerializeField] private float _punchDuration = 0.16f;

    [SerializeField]
    private int _currentIndex;

    private Action _onStart;
    private Action _onIntroduce;
    private Action _onQuit;

    public void Init(
        Action onStart,
        Action onIntroduce,
        Action onQuit)
    {
        _onStart = onStart;
        _onIntroduce = onIntroduce;
        _onQuit = onQuit;
    }

    private void Start()
    {
        if (_buttons == null || _buttons.Length == 0)
        {
            return;
        }

        _selectButtons = new UISelectButton[_buttons.Length];

        for (int i = 0; i < _buttons.Length; i++)
        {
            _selectButtons[i] = _buttons[i].GetComponent<UISelectButton>();
        }

        _currentIndex = 0;

        for (int i = 0; i < _selectButtons.Length; i++)
        {
            _selectButtons[i].SetSelected(_currentIndex == i);
        }
    }

    public void MoveUp()
    {
        MoveTo(_currentIndex - 1);
    }

    public void MoveDown()
    {
        MoveTo(_currentIndex + 1);
    }

    public void MoveTo(int index)
    {
        if (_buttons == null || _buttons.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, _buttons.Length - 1);

        if (_currentIndex == index)
        {
            return;
        }

        _currentIndex = index;
        AudioManager.Instance.PlaySfx(AudioCueId.UiMove);

        for (int i = 0; i < _selectButtons.Length; i ++)
        {
            _selectButtons[i].SetSelected(_currentIndex == i);
        }
    }

    public void Confirm()
    {
        //AudioManager.Instance.PlaySfx(AudioCueId.UiClick);
        AudioManager.Instance.PlaySfx(AudioCueId.UiConfirm);

        _selectButtons[_currentIndex].PlayConfirm();

        switch (_currentIndex)
        {
            case 0:
                _onIntroduce?.Invoke();
                break;

            case 1:
                _onStart?.Invoke();
                break;

            case 2:
                _onQuit?.Invoke();
                break;
            default:
                break;
        }
    }
}
