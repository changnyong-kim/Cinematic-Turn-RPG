using UnityEngine;

public sealed class BattleCinematicShakeCamera : MonoBehaviour
{
    [Header("Camera Shake")]
    [SerializeField]
    private SimpleCameraShake _cameraShake;

    [SerializeField]
    private float _parryShakeDuration = 0.12f;

    [SerializeField]
    private float _parryShakeStrength = 0.07f;

    [SerializeField]
    private float _counterShakeDuration = 0.1f;

    [SerializeField]
    private float _counterShakeStrength = 0.12f;

    [Header("Camera Zoom")]
    [SerializeField]
    private float _parryZoomAmount = 10f;

    [SerializeField]
    private float _parryZoomInDuration = 0.04f;

    [SerializeField]
    private float _parryZoomOutDuration = 0.12f;

    [SerializeField]
    private float _counterZoomAmount = 30f;

    [SerializeField]
    private float _counterZoomInDuration = 0.035f;

    [SerializeField]
    private float _counterZoomOutDuration = 0.14f;

    public void PlayParryReaction(Vector3 battleCenter)
    {
        if (_cameraShake == null)
        {
            return;
        }

        _cameraShake.Shake(_parryShakeDuration, _parryShakeStrength);

        _cameraShake.ZoomPunch(
            _parryZoomAmount,
            _parryZoomInDuration,
            _parryZoomOutDuration);

        _cameraShake.MovePunchToTarget(
            battleCenter,
            0.25f,
            0.04f,
            0.12f);
    }

    public void PlayCounterImpact(Vector3 battleCenter)
    {
        if (_cameraShake == null)
        {
            return;
        }

        _cameraShake.MovePunchToTarget(
            battleCenter,
            0.45f,
            0.035f,
            0.14f);

        _cameraShake.Shake(_counterShakeDuration, _counterShakeStrength);

        _cameraShake.ZoomPunch(
            _counterZoomAmount,
            _counterZoomInDuration,
            _counterZoomOutDuration);
    }
}
