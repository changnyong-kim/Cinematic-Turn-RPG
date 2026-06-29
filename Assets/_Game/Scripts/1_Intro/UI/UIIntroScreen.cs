using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIIntroScreen : MonoBehaviour
    {
        [Header("메뉴 연출")]
        [SerializeField]
        private UIMenuSelector _menuSelector;

        [SerializeField]
        private GameObject _introduce;

        [SerializeField]
        private GameObject _introduceModel, _menuModel;

        [SerializeField]
        private CanvasGroup _fadeOverlayCanvasGroup;

        [SerializeField]
        private float _defaultFadeOutDuration = 0.2f;

        [SerializeField]
        private float _defaultFadeInDuration = 0.25f;

        private bool _isTransitioning;

        public bool IsShowIntroduce
        {
            get;
            private set;
        }

        [SerializeField]
        private IntroSkyboxController _skyboxController;

        [Header("Error Popup")]
        [SerializeField]
        private GameObject _errorPopup;

        [SerializeField]
        private Text _errorMessageText;

        [Header("Need Download Popup")]
        [SerializeField]
        private GameObject _needDownloadPopup;

        [SerializeField]
        private Text _needDownloadMessageText;

        [Header("Download Progress")]
        [SerializeField]
        private GameObject _downloadProgressPopup;

        [SerializeField]
        private Slider _downloadProgressSlider;

        [SerializeField]
        private Text _downloadProgressText;

        public UIMenuSelector MenuSelector { get { return _menuSelector; } }

        private void Awake()
        {
            CloseErrorPopup();
            CloseNeedDownloadPopup();
            CloseDownloadProgress();
        }

        
        public void Init(
            Action onStart,
            Action onIntroduce,
            Action onQuit)
        {
            MenuSelector.Init(onStart, OnIntroduce, onQuit);

            ShowMenu();
        }

        #region 메뉴
        private void OnIntroduce()
        {
            IsShowIntroduce = true;
            OpenIntroduceAsync().Forget();
        }

        public void CloseIntroduce()
        {
            IsShowIntroduce = false;
            CloseIntroduceAsync().Forget();
        }

        private void ShowMenu()
        {
            _introduceModel.SetActive(false);
            _menuModel.gameObject.SetActive(true);
            _menuSelector.gameObject.SetActive(true);
            _introduce.gameObject.SetActive(false);
        }

        private void ShowIntroduce()
        {
            _introduceModel.SetActive(true);
            _menuModel.gameObject.SetActive(false);
            _menuSelector.gameObject.SetActive(false);
            _introduce.gameObject.SetActive(true);
        }

        public async UniTask OpenIntroduceAsync()
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;

            if (_skyboxController != null)
            {
                await _skyboxController.ChangeToIntroduceSkyboxAsync();
            }

            await FadeOverlayAsync(0f, 0.9f, _defaultFadeOutDuration);

            ShowIntroduce();

            await FadeOverlayAsync(0.9f, 0f, _defaultFadeInDuration);

            _isTransitioning = false;
        }

        public async UniTask CloseIntroduceAsync()
        {
            _isTransitioning = true;

            if (_skyboxController != null)
            {
                await _skyboxController.ChangeToMenuSkyboxAsync();
            }

            await FadeOverlayAsync(0f, 0.9f, 0.18f);

            ShowMenu();

            await FadeOverlayAsync(0.9f, 0f, 0.22f);

            _isTransitioning = false;
        }
        #endregion

        public void OpenErrorPopup(string message)
        {
            if (_errorPopup != null)
            {
                _errorPopup.SetActive(true);
            }

            if (_errorMessageText != null)
            {
                _errorMessageText.text = message;
            }

            Debug.LogError($"[UIIntroScreen] Error Popup: {message}");
        }

        public void CloseErrorPopup()
        {
            if (_errorPopup != null)
            {
                _errorPopup.SetActive(false);
            }
        }


        public void OpenNeedDownloadPopup(long downloadBytes)
        {
            Debug.Log($"[UIIntroScreen] Need Download Popup. Bytes: {downloadBytes}");

            if (_needDownloadPopup != null)
            {
                _needDownloadPopup.SetActive(true);
            }

            if (_needDownloadMessageText != null)
            {
                if (downloadBytes > 0)
                {
                    _needDownloadMessageText.text = $"추가 다운로드가 필요합니다.\n용량: {FormatBytes(downloadBytes)}";
                }
                else
                {
                    _needDownloadMessageText.text = "추가 다운로드가 필요합니다.";
                }
            }
        }

        public void CloseNeedDownloadPopup()
        {
            if (_needDownloadPopup != null)
            {
                _needDownloadPopup.SetActive(false);
            }
        }

        public void OpenDownloadProgress()
        {
            if (_downloadProgressPopup != null)
            {
                _downloadProgressPopup.SetActive(true);
            }

            SetDownloadProgress(0f);
        }

        public void SetDownloadProgress(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);

            if (_downloadProgressSlider != null)
            {
                _downloadProgressSlider.value = clampedProgress;
            }

            if (_downloadProgressText != null)
            {
                _downloadProgressText.text = $"{clampedProgress * 100f:0}%";
            }
        }

        public void CloseDownloadProgress()
        {
            if (_downloadProgressPopup != null)
            {
                _downloadProgressPopup.SetActive(false);
            }
        }

        private string FormatBytes(long bytes)
        {
            const double kiloByte = 1024d;
            const double megaByte = kiloByte * 1024d;
            const double gigaByte = megaByte * 1024d;

            if (bytes >= gigaByte)
            {
                return $"{bytes / gigaByte:0.##} GB";
            }

            if (bytes >= megaByte)
            {
                return $"{bytes / megaByte:0.##} MB";
            }

            if (bytes >= kiloByte)
            {
                return $"{bytes / kiloByte:0.##} KB";
            }

            return $"{bytes} B";
        }

        #region 페이드인페이드아웃 연출
        private async UniTask FadeOverlayAsync(float fromAlpha, float toAlpha, float duration)
            {
                if (_fadeOverlayCanvasGroup == null)
                {
                    return;
                }

                _fadeOverlayCanvasGroup.gameObject.SetActive(true);
                _fadeOverlayCanvasGroup.blocksRaycasts = true;
                _fadeOverlayCanvasGroup.interactable = true;

                float elapsed = 0f;
                _fadeOverlayCanvasGroup.alpha = fromAlpha;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;

                    float t = Mathf.Clamp01(elapsed / duration);

                    // EaseOutQuad 느낌
                    t = 1f - Mathf.Pow(1f - t, 2f);

                    _fadeOverlayCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                _fadeOverlayCanvasGroup.alpha = toAlpha;

                if (Mathf.Approximately(toAlpha, 0f))
                {
                    _fadeOverlayCanvasGroup.blocksRaycasts = false;
                    _fadeOverlayCanvasGroup.interactable = false;
                    _fadeOverlayCanvasGroup.gameObject.SetActive(false);
                }
            }
        }
        #endregion
}