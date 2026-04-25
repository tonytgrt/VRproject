using UnityEngine;
using UnityEngine.Events;

namespace IcePEAK.Player
{
    /// <summary>
    /// Trigger volume that, when entered by the player, becomes the new
    /// respawn target for the FallHandler. Place one collider per checkpoint
    /// (set Is Trigger), assign the FallHandler, and pick a player layer mask.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("FallHandler whose spawn point will be updated on activation.")]
        [SerializeField] private FallHandler fallHandler;
        [Tooltip("Optional. Transform used as the spawn pose. If null, this checkpoint's own transform is used.")]
        [SerializeField] private Transform spawnTransform;

        [Header("Detection")]
        [Tooltip("Layers that count as the player. The XR Origin (or any of its colliders) should be on one of these.")]
        [SerializeField] private LayerMask playerLayer = ~0;

        [Header("Behavior")]
        [Tooltip("If true, the checkpoint can only be activated once per play session.")]
        [SerializeField] private bool activateOnce = true;

        [Header("Events")]
        [Tooltip("Fired the first time this checkpoint is activated. Hook VFX/SFX here.")]
        [SerializeField] private UnityEvent onActivated;

        private bool _activated;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activateOnce && _activated) return;
            if (fallHandler == null) return;
            if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

            _activated = true;
            fallHandler.SetCheckpoint(spawnTransform != null ? spawnTransform : transform);
            onActivated?.Invoke();
        }
    }
}
