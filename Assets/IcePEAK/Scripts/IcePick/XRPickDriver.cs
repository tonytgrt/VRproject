using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads XR tracking data and stores it. Does NOT position the pick directly.
/// IcePickController reads the stored target in FixedUpdate and drives the
/// rigidbody via velocity so physics collisions with rock are respected.
/// When the pick is embedded, disable this to stop updating the target.
/// </summary>
public class XRPickDriver : MonoBehaviour
{
    [Header("XR Input (same actions as the controller's TrackedPoseDriver)")]
    [SerializeField] private InputActionReference positionAction;
    [SerializeField] private InputActionReference rotationAction;

    [Header("Space Reference")]
    [Tooltip("The Camera Offset transform under XR Origin — tracking data is local to this")]
    [SerializeField] private Transform cameraOffset;

    [Header("Grip Offset")]
    [Tooltip("Position offset from the controller origin to the pick grip (local space)")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 0.08f);
    [Tooltip("Rotation offset from the controller to the pick grip (Euler angles)")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(45f, 0f, 0f);

    private Quaternion _rotOffsetQuat;

    // Stored tracking-space data (does not change with XR Origin movement)
    private Vector3 _trackingPos;
    private Quaternion _trackingRot = Quaternion.identity;

    private void Awake()
    {
        _rotOffsetQuat = Quaternion.Euler(rotationOffset);
    }

    private void OnEnable()
    {
        if (positionAction != null && positionAction.action != null)
            positionAction.action.Enable();
        if (rotationAction != null && rotationAction.action != null)
            rotationAction.action.Enable();

        InputSystem.onAfterUpdate += ReadTrackingData;
    }

    private void OnDisable()
    {
        InputSystem.onAfterUpdate -= ReadTrackingData;
    }

    /// <summary>
    /// Called by InputSystem at the same timing as TrackedPoseDriver.
    /// Only READS and STORES tracking data — does not move the pick.
    /// </summary>
    private void ReadTrackingData()
    {
        if (positionAction == null || rotationAction == null) return;

        _trackingPos = positionAction.action.ReadValue<Vector3>();
        _trackingRot = rotationAction.action.ReadValue<Quaternion>();
    }

    /// <summary>
    /// Computes the current world-space target position and rotation
    /// using the CURRENT Camera Offset transform. Call this in FixedUpdate
    /// (after ClimbingLocomotion has moved the XR Origin) to get the correct target.
    /// </summary>
    public void GetWorldTarget(out Vector3 worldPos, out Quaternion worldRot)
    {
        worldPos = cameraOffset.TransformPoint(_trackingPos);
        worldRot = cameraOffset.rotation * _trackingRot;

        // Apply grip offset
        worldPos += worldRot * positionOffset;
        worldRot *= _rotOffsetQuat;
    }
}
