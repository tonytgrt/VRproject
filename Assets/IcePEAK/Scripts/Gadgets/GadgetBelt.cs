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
            // World-space yaw only — no pitch, no roll, no bobbing when looking up/down.
            transform.rotation = Quaternion.Euler(0f, hmd.eulerAngles.y, 0f);
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
