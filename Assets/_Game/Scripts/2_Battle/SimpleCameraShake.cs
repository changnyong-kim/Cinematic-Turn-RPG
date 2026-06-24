using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class SimpleCameraShake : MonoBehaviour
{
    [SerializeField]
    private Camera _targetCamera;

    private Vector3 _originLocalPosition;
    private float _originFieldOfView;
    private bool _isShaking;
    private bool _isZooming;

    private void Awake()
    {
        _originLocalPosition = transform.localPosition;

        if (_targetCamera == null)
        {
            _targetCamera = GetComponent<Camera>();
        }

        if (_targetCamera != null)
        {
            _originFieldOfView = _targetCamera.fieldOfView;
        }
    }

    public void Shake(float duration, float strength)
    {
        ShakeAsync(duration, strength).Forget();
    }

    public void ZoomPunch(float zoomInAmount, float zoomInDuration, float zoomOutDuration)
    {
        ZoomPunchAsync(zoomInAmount, zoomInDuration, zoomOutDuration).Forget();
    }

    private async UniTask ShakeAsync(float duration, float strength)
    {
        if (_isShaking)
        {
            return;
        }

        _isShaking = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float ratio = 1f - Mathf.Clamp01(elapsed / duration);
            Vector3 randomOffset = Random.insideUnitSphere * strength * ratio;
            randomOffset.z = 0f;

            transform.localPosition = _originLocalPosition + randomOffset;

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        transform.localPosition = _originLocalPosition;
        _isShaking = false;
    }

    private async UniTask ZoomPunchAsync(
        float zoomInAmount,
        float zoomInDuration,
        float zoomOutDuration)
    {
        if (_targetCamera == null)
        {
            return;
        }

        if (_isZooming)
        {
            return;
        }

        _isZooming = true;

        float startFov = _originFieldOfView;
        float targetFov = _originFieldOfView - zoomInAmount;

        await LerpFovAsync(startFov, targetFov, zoomInDuration);
        await LerpFovAsync(targetFov, startFov, zoomOutDuration);

        _targetCamera.fieldOfView = _originFieldOfView;
        _isZooming = false;
    }

    private async UniTask LerpFovAsync(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 2f);

            _targetCamera.fieldOfView = Mathf.Lerp(from, to, t);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _targetCamera.fieldOfView = to;
    }

    public void MovePunchToTarget(
    Vector3 targetPosition,
    float moveDistance,
    float moveInDuration,
    float moveOutDuration)
    {
        MovePunchToTargetAsync(
            targetPosition,
            moveDistance,
            moveInDuration,
            moveOutDuration).Forget();
    }

    private async UniTask MovePunchToTargetAsync(
        Vector3 targetPosition,
        float moveDistance,
        float moveInDuration,
        float moveOutDuration)
    {
        Vector3 startPosition = transform.position;

        Vector3 direction = targetPosition - startPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 targetCameraPosition =
            startPosition + direction.normalized * moveDistance;

        await LerpPositionAsync(startPosition, targetCameraPosition, moveInDuration);
        await LerpPositionAsync(targetCameraPosition, startPosition, moveOutDuration);

        transform.position = startPosition;
    }

    private async UniTask LerpPositionAsync(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 2f);

            transform.position = Vector3.Lerp(from, to, t);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        transform.position = to;
    }
}