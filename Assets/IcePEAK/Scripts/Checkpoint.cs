using UnityEngine;

/// <summary>
/// Place on an empty GameObject at each safe ledge along the cliff.
/// Requires a Collider set to <b>Is Trigger = true</b>.
///
/// When the player enters the trigger, this becomes the active checkpoint.
/// FallingRock reads <see cref="LastPosition"/> to know where to respawn.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    /// <summary>World position of the most recently reached checkpoint.</summary>
    public static Vector3 LastPosition { get; private set; }

    /// <summary>True once the player has reached at least one checkpoint.</summary>
    public static bool HasCheckpoint { get; private set; }

    [Tooltip("Offset from this transform's position used as the actual respawn point. " +
             "Useful to land the player slightly in front of or above the trigger center.")]
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        LastPosition = transform.position + respawnOffset;
        HasCheckpoint = true;
        Debug.Log($"[Checkpoint] Saved at {LastPosition}");
    }

    private void OnDrawGizmos()
    {
        Vector3 spawnPos = transform.position + respawnOffset;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(spawnPos, 0.2f);
        if (respawnOffset != Vector3.zero)
            Gizmos.DrawLine(transform.position, spawnPos);
    }
}
