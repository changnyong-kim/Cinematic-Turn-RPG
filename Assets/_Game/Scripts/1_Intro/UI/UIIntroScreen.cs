using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIIntroScreen : MonoBehaviour
    {
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

        private void Awake()
        {
            CloseErrorPopup();
            CloseNeedDownloadPopup();
            CloseDownloadProgress();
        }

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
    }
}