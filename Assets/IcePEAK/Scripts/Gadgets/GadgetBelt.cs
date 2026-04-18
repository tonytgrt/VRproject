using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Waist-height belt. Parent to the XR Origin (XR Rig) root, NOT Camera Offset.
    /// Position is static in the prefab; only yaw is updated each LateUpdate from the HMD.
    /// </summary>
    public class GadgetBelt : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Main Camera (HMD) transform — belt yaw tracks this.")]
        [SerializeField] private Transform hmd;

        [Tooltip("All belt slots, in the order you want them iterated.")]
        [SerializeField] private BeltSlot[] slots;

        [Header("Tunables")]
        [Tooltip("Max hand→slot distance that counts as 'hovered'. Meters.")]
        [SerializeField] private float proximityRadius = 0.15f;

        public BeltSlot[] Slots => slots;
        public float ProximityRadius => proximityRadius;

        private void LateUpdate()
        {
            if (hmd == null) return;
            // Derive yaw from a flattened forward vector, not eulerAngles.y — the latter
            // jitters/flips near pitch = ±90° (gimbal lock), which is common in ice climbing
            // where the player looks straight up at a route or straight down at their feet.
            Vector3 fwd = hmd.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f)
            {
                // Pure vertical gaze — fall back to the HMD's up vector (top of head points
                // opposite of view direction when looking straight up/down).
                fwd = hmd.up;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f) return;
            }
            transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        /// <summary>
        /// Nearest slot to <paramref name="handWorldPos"/> within proximityRadius.
        /// Deterministic: returns the single closest slot, or null if none in range.
        /// </summary>
        public bool TryGetNearestSlot(Vector3 handWorldPos, out BeltSlot nearest)
        {
            nearest = null;
            if (slots == null) return false;

            float bestSqr = proximityRadius * proximityRadius;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                float sqr = (s.Anchor.position - handWorldPos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = s;
                }
            }
            return nearest != null;
        }
    }
}
