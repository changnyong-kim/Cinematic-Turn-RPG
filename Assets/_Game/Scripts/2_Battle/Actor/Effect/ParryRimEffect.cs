using DG.Tweening;
using UnityEngine;

public sealed class ParryRimEffect : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    [Header("Material")]
    [SerializeField]
    private Material _rimMaterial;

    [Header("Timing")]
    [SerializeField]
    private float _fadeInDuration = 0.04f;

    [SerializeField]
    private float _holdDuration = 0.08f;

    [SerializeField]
    private float _fadeOutDuration = 0.16f;

    [Header("Intensity")]
    [SerializeField]
    private float _peakIntensity = 1.2f;

    private Tween _rimTween;

    private void Awake()
    {
        SetIntensity(0f);
    }

    public void Play()
    {
        if (_rimMaterial == null)
        {
            return;
        }

        _rimTween?.Kill();

        SetIntensity(0f);

        Sequence sequence = DOTween.Sequence();

        sequence
            .SetUpdate(true)
            .Append(
                DOTween.To(
                        SetIntensity,
                        0f,
                        _peakIntensity,
                        _fadeInDuration)
                    .SetEase(Ease.OutQuad))
            .AppendInterval(_holdDuration)
            .Append(
                DOTween.To(
                        SetIntensity,
                        _peakIntensity,
                        0f,
                        _fadeOutDuration)
                    .SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                SetIntensity(0f);
                _rimTween = null;
            });

        _rimTween = sequence;
    }

    public void Stop()
    {
        if (_rimMaterial == null)
        {
            return;
        }

        _rimTween?.Kill();

        float currentIntensity =
            _rimMaterial.GetFloat(IntensityId);

        _rimTween = DOTween.To(
                SetIntensity,
                currentIntensity,
                0f,
                _fadeOutDuration)
            .SetUpdate(true)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                SetIntensity(0f);
                _rimTween = null;
            });
    }

    private void SetIntensity(float intensity)
    {
        if (_rimMaterial == null)
        {
            return;
        }

        _rimMaterial.SetFloat(IntensityId, intensity);
    }

    private void OnDisable()
    {
        _rimTween?.Kill();
        _rimTween = null;

        SetIntensity(0f);
    }

    private void OnDestroy()
    {
        _rimTween?.Kill();
        _rimTween = null;

        SetIntensity(0f);
    }
}
