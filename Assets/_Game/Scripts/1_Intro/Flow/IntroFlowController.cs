using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Intro.Flow
{
    public sealed class IntroFlowController : MonoBehaviour
    {
        private sealed class DownloadCheckResult
        {
            public bool IsSuccess
            {
                get;
            }

            public long AddressableDownloadBytes
            {
                get;
            }

            public string ErrorMessage
            {
                get;
            }

            public bool NeedDownload => AddressableDownloadBytes > 0;
            public long TotalDownloadBytes => AddressableDownloadBytes;

            private DownloadCheckResult(
                bool isSuccess,
                long addressableDownloadBytes,
                string errorMessage)
            {
                IsSuccess = isSuccess;
                AddressableDownloadBytes = addressableDownloadBytes;
                ErrorMessage = errorMessage;
            }

            public static DownloadCheckResult Success(long addressableDownloadBytes)
            {
                return new DownloadCheckResult(true, addressableDownloadBytes, string.Empty);
            }

            public static DownloadCheckResult Fail(System.Exception exception)
            {
                return new DownloadCheckResult(false, 0, exception.Message);
            }
        }

        [SerializeField]
        private UI.UIIntroScreen _introScreen;

        private IntroPlatformBase _platformStrategy;

        private void Awake()
        {
            _platformStrategy = CreatePlatformStrategy();
        }

        private void Start()
        {
            StartBootFlowAsync().Forget(Debug.LogException);
        }

        private IntroPlatformBase CreatePlatformStrategy()
        {
#if UNITY_EDITOR
            return new IntroPlatformEditor();
#elif UNITY_ANDROID || UNITY_IOS
            return new IntroPlatformMobile();
#else
            return new IntroPlatformPC();
#endif
        }

        private async UniTask StartBootFlowAsync()
        {
            await _platformStrategy.InitializeAsync();

            bool tableLoadSuccess = LoadTables();

            if (tableLoadSuccess == false)
            {
                // TODO: 테이블 로드 실패 팝업
                return;
            }

            await InitializeAddressablesAsync();

            DownloadCheckResult downloadCheckResult = await CheckAddressableDownloadAsync();

            if (downloadCheckResult.IsSuccess == false)
            {
                // TODO: 다운로드 체크 실패 팝업
                Debug.Log("다운로드 체크 실패 팝업");
                return;
            }

            if (downloadCheckResult.NeedDownload)
            {
                _introScreen.OpenNeedDownloadPopup(downloadCheckResult.AddressableDownloadBytes);
                return;
            }

            _introScreen.Init(
                onStart: () => EnterNextSceneAsync().Forget(),
                () => { },
                onQuit: QuitApplication);
        }

        #region 유저 입력
        public void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.wKey.wasReleasedThisFrame ||
                Keyboard.current.upArrowKey.wasReleasedThisFrame)
            {
                _introScreen.MenuSelector.MoveUp();
            }

            if (Keyboard.current.sKey.wasReleasedThisFrame ||
                Keyboard.current.downArrowKey.wasReleasedThisFrame)
            {
                _introScreen.MenuSelector.MoveDown();
            }

            if (Keyboard.current.enterKey.wasReleasedThisFrame ||
                Keyboard.current.numpadEnterKey.wasReleasedThisFrame)
            {
                _introScreen.MenuSelector.Confirm();
            }

            if (Keyboard.current.escapeKey.wasReleasedThisFrame &&
                _introScreen.IsShowIntroduce)
            {
                _introScreen.CloseIntroduceAsync().Forget();
            }
        }
        #endregion


        private bool LoadTables()
        {
            if (TableManager.Instance == null)
            {
                Debug.LogError("[IntroFlow] TableManager is null.");
                return false;
            }

            return TableManager.Instance.LoadTables();
        }

        private async UniTask InitializeAddressablesAsync()
        {
            await Addressables.InitializeAsync().ToUniTask();
        }

        private async UniTask<DownloadCheckResult> CheckAddressableDownloadAsync()
        {
            try
            {
                List<string> catalogs = await Addressables.CheckForCatalogUpdates().ToUniTask();

                if (catalogs != null && catalogs.Count > 0)
                {
                    await Addressables.UpdateCatalogs(catalogs, false).ToUniTask();
                }

                List<object> requiredKeys = AssetManager.Instance.DownloadKeyPreloadKeyProvider.CollectRequiredAddressableKeys();

                if (requiredKeys.Count <= 0)
                {
                    Debug.LogWarning("[IntroFlow] Required addressable key list is empty.");
                    return DownloadCheckResult.Success(0);
                }

                long downloadSize = await GetAddressableDownloadSizeAsync(requiredKeys);

                return DownloadCheckResult.Success(downloadSize);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return DownloadCheckResult.Fail(exception);
            }
        }

        private async UniTask<long> GetAddressableDownloadSizeAsync(List<object> keys)
        {
            long downloadSize = await Addressables.GetDownloadSizeAsync(keys).ToUniTask();

            return downloadSize;
        }

        private async UniTask EnterNextSceneAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(1));

            await SceneManager.LoadSceneAsync("1_Game").ToUniTask();
        }

        public void QuitApplication()
        {
            _platformStrategy.QuitApplication();
        }
    }
}