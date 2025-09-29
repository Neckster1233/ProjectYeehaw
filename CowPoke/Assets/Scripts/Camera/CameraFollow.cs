using UnityEngine;

/// <summary>
/// Simple camera follow script.
/// - Follows the target's position + world-space offset.
/// - Keeps a fixed camera rotation (does not rotate with the player).
/// - Smooths position using SmoothDamp. Use LateUpdate to avoid jitter.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Transform to follow (usually the player)")]
    public Transform target;

    [Tooltip("World-space offset from the target. Keep this in world space so camera won't rotate with player.")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);

    [Tooltip("Smoothing time in seconds. 0 = no smoothing (instant follow).")]
    public float smoothTime = 0.12f;

    [Tooltip("If true, camera will maintain a fixed Y value (locked height) regardless of target Y)")]
    public bool lockY = false;
    [Tooltip("Height used when lockY is enabled.")]
    public float lockedY = 12f;

    Vector3 velocity = Vector3.zero;
    Quaternion initialRotation;

    void Awake()
    {
        // store rotation so we can keep it constant
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (lockY) desired.y = lockedY;

        if (smoothTime > 0f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
        else
        {
            transform.position = desired;
        }

        // Always keep the camera's rotation fixed (do not inherit player's rotation)
        transform.rotation = initialRotation;
    }
}
