using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public sealed class BattleCameraRig : MonoBehaviour
{
    [Serializable]
    private struct CameraPose
    {
        public Vector3 RigPosition;
        public Vector3 ArmEulerAngles;
        public float CameraDistance;
        public float FieldOfView;
    }

    [Serializable]
    private struct CameraShot
    {
        [Tooltip("Base Rig Position 기준 추가 이동값")]
        public Vector3 RigOffset;

        [Tooltip("공격자와 방어자 사이 포커스 높이")]
        public float FocusHeight;

        [Range(0f, 1f)]
        [Tooltip("0 = 공격자 중심, 0.5 = 중간, 1 = 방어자 중심")]
        public float FocusWeight;

        [Tooltip("CameraSocket의 Local Z 거리")]
        public float CameraDistance;

        public float FieldOfView;
        public float Duration;
    }

    [Header("References")]
    [SerializeField]
    private Transform _cameraArm;

    [SerializeField]
    private Transform _cameraSocket;

    [SerializeField]
    private Transform _lookTarget;

    [SerializeField]
    private Camera _targetCamera;

    [Header("Base Pose")]
    [SerializeField]
    private Vector3 _baseRigPosition = new Vector3(0.06f, 1.2f, -2.4f);

    [SerializeField]
    private Vector3 _baseArmEulerAngles = new Vector3(5.82f, 0f, 0f);

    [SerializeField]
    private float _baseCameraDistance = -7f;

    [SerializeField]
    private float _baseFieldOfView = 65f;

    [Header("Player Move Camera")]
    [SerializeField]
    private CameraShot _playerMoveShot = new CameraShot
    {
        RigOffset = new Vector3(-1.4f, 0.35f, -1.9f),
        FocusHeight = 1.1f,
        FocusWeight = 0.25f,
        CameraDistance = -4.2f,
        FieldOfView = 48f,
        Duration = 0.35f
    };

    [Header("Player Attack Camera")]
    [SerializeField]
    private CameraShot _playerAttackShot = new CameraShot
    {
        RigOffset = new Vector3(-2f, 0.5f, -2.5f),
        FocusHeight = 1.15f,
        FocusWeight = 0.45f,
        CameraDistance = -3.2f,
        FieldOfView = 42f,
        Duration = 0.08f
    };

    [Header("Monster Move Camera")]
    [SerializeField]
    private CameraShot _monsterMoveShot = new CameraShot
    {
        RigOffset = new Vector3(-1.2f, 0.15f, -1.8f),
        FocusHeight = 1.05f,
        FocusWeight = 0.5f,
        CameraDistance = -4.2f,
        FieldOfView = 45f,
        Duration = 0.35f
    };

    [Header("Monster Attack Camera")]
    [SerializeField]
    private CameraShot _monsterAttackShot = new CameraShot
    {
        RigOffset = new Vector3(-1.2f, 0.1f, -1.8f),
        FocusHeight = 1.0f,
        FocusWeight = 0.55f,
        CameraDistance = -4.0f,
        FieldOfView = 45f,
        Duration = 0.08f
    };

    [Header("Parry Success Camera")]
    [SerializeField]
    private CameraShot _parrySuccessShot = new CameraShot
    {
        RigOffset = new Vector3(-1.5f, 0.05f, -1.7f),
        FocusHeight = 1.05f,
        FocusWeight = 0.6f,
        CameraDistance = -3.8f,
        FieldOfView = 42f,
        Duration = 0.08f
    };

    [Header("Return")]
    [SerializeField]
    private float _returnDuration = 0.45f;

    private CameraPose _basePose;
    private Transform _focusAttacker;
    private Transform _focusDefender;
    private CameraShot _activeLookShot;
    private bool _useLookTarget;
    private CancellationTokenSource _moveCts;

    private void Awake()
    {
        ResolveReferences();
        RebuildBasePose();
        ApplyFullPoseImmediate(_basePose);
    }

    private void LateUpdate()
    {
        if (_useLookTarget == false)
        {
            return;
        }

        UpdateLookTargetPosition();
        RotateArmToLookTarget();
    }

    public void PlayMoveCamera(BattleTeam attackerTeam, ActorBase attacker, ActorBase defender)
    {
        CameraShot shot = GetMoveShot(attackerTeam);
        PlayLookTargetShotAsync(shot, attacker, defender).Forget(Debug.LogException);
    }

    public void PlayAttackCamera(BattleTeam attackerTeam, ActorBase attacker, ActorBase defender)
    {
        CameraShot shot = GetAttackShot(attackerTeam);
        PlayLookTargetShotAsync(shot, attacker, defender).Forget(Debug.LogException);
    }

    public void PlayParrySuccessCamera(ActorBase attacker, ActorBase defender)
    {
        PlayLookTargetShotAsync(_parrySuccessShot, attacker, defender).Forget(Debug.LogException);
    }

    public void ReturnToBase()
    {
        ReturnToBaseAsync().Forget(Debug.LogException);
    }

    public void ApplyBaseImmediate()
    {
        RebuildBasePose();
        StopLookTarget();
        CancelCurrentMove();
        ApplyFullPoseImmediate(_basePose);
    }

    private async UniTask PlayLookTargetShotAsync(
        CameraShot shot,
        ActorBase attacker,
        ActorBase defender)
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        CancellationToken token = CreateMoveToken();

        _focusAttacker = attacker.transform;
        _focusDefender = defender.transform;
        _activeLookShot = shot;
        _useLookTarget = true;

        UpdateLookTargetPosition();
        RotateArmToLookTarget();

        CameraPose targetPose = CaptureCurrentPose();
        targetPose.RigPosition = _baseRigPosition + shot.RigOffset;
        targetPose.CameraDistance = shot.CameraDistance;
        targetPose.FieldOfView = shot.FieldOfView;

        try
        {
            await MoveRigAndCameraAsync(targetPose, shot.Duration, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async UniTask ReturnToBaseAsync()
    {
        CancellationToken token = CreateMoveToken();

        RebuildBasePose();
        StopLookTarget();

        try
        {
            await MoveFullPoseAsync(_basePose, _returnDuration, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private CameraShot GetMoveShot(BattleTeam attackerTeam)
    {
        if (attackerTeam == BattleTeam.Ally)
        {
            return _playerMoveShot;
        }

        if (attackerTeam == BattleTeam.Enemy)
        {
            return _monsterMoveShot;
        }

        return _playerMoveShot;
    }

    private CameraShot GetAttackShot(BattleTeam attackerTeam)
    {
        if (attackerTeam == BattleTeam.Ally)
        {
            return _playerAttackShot;
        }

        if (attackerTeam == BattleTeam.Enemy)
        {
            return _monsterAttackShot;
        }

        return _playerAttackShot;
    }

    private void UpdateLookTargetPosition()
    {
        if (_lookTarget == null)
        {
            return;
        }

        if (_focusAttacker == null || _focusDefender == null)
        {
            return;
        }

        Vector3 focusPosition = Vector3.Lerp(
            _focusAttacker.position,
            _focusDefender.position,
            _activeLookShot.FocusWeight);

        focusPosition += Vector3.up * _activeLookShot.FocusHeight;

        _lookTarget.position = focusPosition;
    }

    private void RotateArmToLookTarget()
    {
        if (_cameraArm == null || _lookTarget == null)
        {
            return;
        }

        Vector3 direction = _lookTarget.position - _cameraArm.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _cameraArm.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private async UniTask MoveRigAndCameraAsync(
        CameraPose targetPose,
        float duration,
        CancellationToken token)
    {
        CameraPose startPose = CaptureCurrentPose();

        if (duration <= 0f)
        {
            ApplyRigAndCameraImmediate(targetPose);
            return;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = EaseOutCubic(t);

            CameraPose pose = LerpPose(startPose, targetPose, t);

            ApplyRigAndCameraImmediate(pose);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        ApplyRigAndCameraImmediate(targetPose);
    }

    private async UniTask MoveFullPoseAsync(
        CameraPose targetPose,
        float duration,
        CancellationToken token)
    {
        CameraPose startPose = CaptureCurrentPose();

        if (duration <= 0f)
        {
            ApplyFullPoseImmediate(targetPose);
            return;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = EaseOutCubic(t);

            CameraPose pose = LerpPose(startPose, targetPose, t);

            ApplyFullPoseImmediate(pose);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        ApplyFullPoseImmediate(targetPose);
    }

    private CameraPose CaptureCurrentPose()
    {
        float cameraDistance = _baseCameraDistance;

        if (_cameraSocket != null)
        {
            cameraDistance = _cameraSocket.localPosition.z;
        }

        float fieldOfView = _baseFieldOfView;

        if (_targetCamera != null)
        {
            fieldOfView = _targetCamera.fieldOfView;
        }

        Vector3 armEulerAngles = _baseArmEulerAngles;

        if (_cameraArm != null)
        {
            armEulerAngles = _cameraArm.localEulerAngles;
        }

        return new CameraPose
        {
            RigPosition = transform.position,
            ArmEulerAngles = armEulerAngles,
            CameraDistance = cameraDistance,
            FieldOfView = fieldOfView
        };
    }

    private CameraPose LerpPose(CameraPose from, CameraPose to, float t)
    {
        return new CameraPose
        {
            RigPosition = Vector3.Lerp(from.RigPosition, to.RigPosition, t),
            ArmEulerAngles = LerpEuler(from.ArmEulerAngles, to.ArmEulerAngles, t),
            CameraDistance = Mathf.Lerp(from.CameraDistance, to.CameraDistance, t),
            FieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t)
        };
    }

    private Vector3 LerpEuler(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(
            Mathf.LerpAngle(from.x, to.x, t),
            Mathf.LerpAngle(from.y, to.y, t),
            Mathf.LerpAngle(from.z, to.z, t));
    }

    private void ApplyFullPoseImmediate(CameraPose pose)
    {
        transform.position = pose.RigPosition;
        transform.rotation = Quaternion.identity;

        if (_cameraArm != null)
        {
            _cameraArm.localPosition = Vector3.zero;
            _cameraArm.localRotation = Quaternion.Euler(pose.ArmEulerAngles);
        }

        ApplyCameraImmediate(pose);
    }

    private void ApplyRigAndCameraImmediate(CameraPose pose)
    {
        transform.position = pose.RigPosition;
        transform.rotation = Quaternion.identity;

        ApplyCameraImmediate(pose);
    }

    private void ApplyCameraImmediate(CameraPose pose)
    {
        if (_cameraSocket != null)
        {
            _cameraSocket.localPosition = new Vector3(0f, 0f, pose.CameraDistance);
            _cameraSocket.localRotation = Quaternion.identity;
        }

        if (_targetCamera != null)
        {
            Transform cameraTransform = _targetCamera.transform;

            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
            _targetCamera.fieldOfView = pose.FieldOfView;
        }
    }

    private void RebuildBasePose()
    {
        _basePose = new CameraPose
        {
            RigPosition = _baseRigPosition,
            ArmEulerAngles = _baseArmEulerAngles,
            CameraDistance = _baseCameraDistance,
            FieldOfView = _baseFieldOfView
        };
    }

    private void StopLookTarget()
    {
        _useLookTarget = false;
        ClearFocusTargets();
    }

    private void ClearFocusTargets()
    {
        _focusAttacker = null;
        _focusDefender = null;
    }

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);

        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private CancellationToken CreateMoveToken()
    {
        CancelCurrentMove();

        _moveCts = new CancellationTokenSource();

        return _moveCts.Token;
    }

    private void CancelCurrentMove()
    {
        if (_moveCts == null)
        {
            return;
        }

        _moveCts.Cancel();
        _moveCts.Dispose();
        _moveCts = null;
    }

    private void ResolveReferences()
    {
        if (_targetCamera == null)
        {
            _targetCamera = GetComponentInChildren<Camera>();
        }

        if (_cameraSocket == null && _targetCamera != null)
        {
            _cameraSocket = _targetCamera.transform.parent;
        }

        if (_cameraArm == null && _cameraSocket != null)
        {
            _cameraArm = _cameraSocket.parent;
        }
    }

    private void OnDestroy()
    {
        CancelCurrentMove();
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Base Pose")]
    private void ApplyBasePoseFromContextMenu()
    {
        RebuildBasePose();
        StopLookTarget();
        ApplyFullPoseImmediate(_basePose);
    }
#endif
}
