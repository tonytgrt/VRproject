using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class IcePickController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SwingDetector swingDetector;
    [SerializeField] private Transform tipTransform;
    [SerializeField] private AudioClip embedSound;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Embed Settings")]
    [Tooltip("How deep the pick tip sinks into the surface on embed (meters)")]
    [SerializeField] private float embedDepth = 0.03f;

    // --- Public API ---
    public bool IsEmbedded => _isEmbedded;
    public Vector3 EmbedWorldPosition => _embedWorldPos;

    /// Invoked when the pick first embeds in an ice surface.
    public System.Action<IcePickController, SurfaceTag> OnEmbedded;

    /// Invoked when the pick is released (grip released or ice shattered).
    public System.Action<IcePickController> OnReleased;

    // --- Private ---
    private bool _isEmbedded;
    private Vector3 _embedWorldPos;
    private Transform _controllerParent;   // original parent (the controller)
    private Vector3 _localPosInParent;     // original local position
    private Quaternion _localRotInParent;   // original local rotation

    private void Awake()
    {
        _controllerParent = transform.parent;
        _localPosInParent = transform.localPosition;
        _localRotInParent = transform.localRotation;
    }

    // --- Trigger Detection ---
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[IcePick] Trigger hit: {other.gameObject.name}, speed={swingDetector.CurrentSpeed:F2}");

        if (_isEmbedded) return;

        SurfaceTag surface = other.GetComponentInParent<SurfaceTag>();
        if (surface == null) return;

        if (surface.Type == SurfaceType.Ice && swingDetector.IsSwingFastEnough)
        {
            Embed(surface);
        }
        else
        {
            Bounce(surface.Type);
        }
    }

    // --- Embed ---
    private void Embed(SurfaceTag surface)
    {
        _isEmbedded = true;

        // Detach from controller so the pick stays fixed in world space
        _embedWorldPos = tipTransform.position;
        transform.SetParent(null, worldPositionStays: true);

        // Nudge the pick slightly into the surface for visual sell
        transform.position += tipTransform.forward * embedDepth;

        // Audio + haptics
        audioSource.PlayOneShot(embedSound);
        // Haptics are sent via the input system (see Section 10)

        OnEmbedded?.Invoke(this, surface);
    }

    // --- Bounce ---
    private void Bounce(SurfaceType type)
    {
        audioSource.PlayOneShot(bounceSound);
        // Spark VFX for rock can be added here later
    }

    // --- Release ---
    public void Release()
    {
        if (!_isEmbedded) return;

        _isEmbedded = false;

        // Re-attach to controller
        transform.SetParent(_controllerParent);
        transform.localPosition = _localPosInParent;
        transform.localRotation = _localRotInParent;

        OnReleased?.Invoke(this);
    }
}