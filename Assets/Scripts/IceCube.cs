using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// An ice cube that can be struck with an IcePick.
/// When the pick is embedded, the cube cracks over time and breaks after breakTime seconds.
/// </summary>
public class IceCube : MonoBehaviour
{
    [Header("Break Settings")]
    [Tooltip("Time in seconds the pick must stay embedded before the cube breaks.")]
    public float breakTime = 2f;

    [Header("Crack Visuals")]
    [Tooltip("Optional: a child object showing crack overlay, enabled on strike.")]
    public GameObject crackOverlay;

    [Tooltip("Optional: material to swap to as cracks progress.")]
    public Material crackedMaterial;

    [Header("Break Effect")]
    [Tooltip("Prefab to spawn when the cube breaks (particle effect, shattered pieces, etc.).")]
    public GameObject breakEffectPrefab;

    [Header("Events")]
    public UnityEvent onStrike;
    public UnityEvent onBreak;

    private float _breakTimer;
    private bool _isStruck;
    private bool _isBroken;
    private IcePick _embeddedPick;
    private Renderer _renderer;
    private Material _originalMaterial;
    private Color _originalColor;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
            _originalColor = _originalMaterial.color;
        }

        if (crackOverlay != null)
            crackOverlay.SetActive(false);
    }

    private void Update()
    {
        if (_isBroken || !_isStruck)
            return;

        _breakTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_breakTimer / breakTime);

        // Visual feedback: tint or lerp material as cracks spread
        UpdateCrackVisuals(progress);

        if (_breakTimer >= breakTime)
        {
            Break();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isBroken || _isStruck)
            return;

        var pick = other.GetComponent<IcePick>();
        if (pick == null)
            pick = other.GetComponentInParent<IcePick>();

        if (pick != null && pick.CurrentSpeed >= pick.minStrikeSpeed)
        {
            Strike(pick);
        }
    }

    private void Strike(IcePick pick)
    {
        _isStruck = true;
        _embeddedPick = pick;
        _breakTimer = 0f;

        if (crackOverlay != null)
            crackOverlay.SetActive(true);

        onStrike.Invoke();
    }

    private void UpdateCrackVisuals(float progress)
    {
        if (_renderer == null)
            return;

        if (crackedMaterial != null)
        {
            // Lerp between original and cracked material color
            _renderer.material.Lerp(_originalMaterial, crackedMaterial, progress);
        }
        else
        {
            // Default: fade to white and increase transparency
            Color c = Color.Lerp(_originalColor, Color.white, progress * 0.5f);
            c.a = Mathf.Lerp(1f, 0.3f, progress);
            _renderer.material.color = c;
        }
    }

    private void Break()
    {
        _isBroken = true;

        onBreak.Invoke();

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, transform.rotation);
        }

        // Destroy the ice cube
        Destroy(gameObject);
    }
}
