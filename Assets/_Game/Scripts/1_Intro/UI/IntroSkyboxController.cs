using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 인트로 씬의 Skybox 색상 전환을 담당한다.
/// 
/// AI 도움을 받아 작성했으며,
/// 프로젝트에 맞게 Cubemap Skybox의 Tint / Exposure 값을
/// 메뉴 화면과 소개 화면 전환에 맞춰 조정하도록 수정했다.
/// 
/// 메인 메뉴와 소개 페이지의 분위기를 분리하기 위한
/// 시각 연출 전용 컨트롤러이다.
/// </summary>
public sealed class IntroSkyboxController : MonoBehaviour
{
    [Header("Menu Skybox")]
    [SerializeField]
    private Color _menuTint = new Color(79f / 255f, 40f / 255f, 45f / 255f, 0.5f);

    [SerializeField]
    private float _menuExposure = 0.45f;

    [Header("Introduce Skybox")]
    [SerializeField]
    private Color _introduceTint = new Color(28f / 255f, 48f / 255f, 88f / 255f, 0.5f);

    [SerializeField]
    private float _introduceExposure = 0.65f;

    [Header("Transition")]
    [SerializeField]
    private float _transitionDuration = 0.35f;

    private Material _skyboxMaterial;

    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");

    private void Awake()
    {
        _skyboxMaterial = RenderSettings.skybox;

        if (_skyboxMaterial == null)
        {
            Debug.LogWarning("[IntroSkyboxController] RenderSettings.skybox is null.");
            return;
        }

        ApplySkybox(_menuTint, _menuExposure);
    }

    public UniTask ChangeToMenuSkyboxAsync()
    {
        return ChangeSkyboxAsync(_menuTint, _menuExposure, _transitionDuration);
    }

    public UniTask ChangeToIntroduceSkyboxAsync()
    {
        return ChangeSkyboxAsync(_introduceTint, _introduceExposure, _transitionDuration);
    }

    [ContextMenu("Test Menu Skybox")]
    private void TestMenuSkybox()
    {
        ApplySkybox(_menuTint, _menuExposure);
    }

    [ContextMenu("Test Introduce Skybox")]
    private void TestIntroduceSkybox()
    {
        ApplySkybox(_introduceTint, _introduceExposure);
    }

    private async UniTask ChangeSkyboxAsync(Color targetTint, float targetExposure, float duration)
    {
        if (_skyboxMaterial == null)
        {
            return;
        }

        Color startTint = _skyboxMaterial.GetColor(TintId);
        float startExposure = _skyboxMaterial.GetFloat(ExposureId);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 2f);

            Color currentTint = Color.Lerp(startTint, targetTint, t);
            float currentExposure = Mathf.Lerp(startExposure, targetExposure, t);

            ApplySkybox(currentTint, currentExposure);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        ApplySkybox(targetTint, targetExposure);
    }

    private void ApplySkybox(Color tint, float exposure)
    {
        if (_skyboxMaterial == null)
        {
            return;
        }

        if (_skyboxMaterial.HasProperty(TintId))
        {
            _skyboxMaterial.SetColor(TintId, tint);
        }

        if (_skyboxMaterial.HasProperty(ExposureId))
        {
            _skyboxMaterial.SetFloat(ExposureId, exposure);
        }

        RenderSettings.skybox = _skyboxMaterial;
    }
}