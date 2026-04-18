using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Single belt slot. Same adoption/initialPrefab pattern as HandCell.
    /// SetHighlighted toggles emissive on the held item's renderers, or on the
    /// placeholder renderer if the slot is empty.
    ///
    /// Convention (same as HandCell): the only design-time children under a BeltSlot
    /// GameObject should be (a) the placeholder renderer wired into placeholderRenderer
    /// and (b) optionally a held item. The placeholder child is explicitly skipped at
    /// Awake so it won't be mis-adopted as HeldItem; any OTHER extra child (debug gizmo,
    /// mesh helper, etc.) WILL be mis-adopted. Keep visuals on the belt parent instead.
    /// </summary>
    public class BeltSlot : MonoBehaviour, ICell
    {
        [Tooltip("If the anchor has no existing child at Awake, instantiate this prefab. Optional.")]
        [SerializeField] private GameObject initialPrefab;

        [Tooltip("Renderer on the empty-slot wireframe placeholder. Highlighted when slot is empty.")]
        [SerializeField] private Renderer placeholderRenderer;

        [Tooltip("Emissive color applied on highlight.")]
        [SerializeField] private Color highlightEmissive = new Color(1f, 0.85f, 0.3f) * 2f;

        public GameObject HeldItem { get; private set; }
        public Transform Anchor => transform;
        public CellKind Kind => CellKind.BeltSlot;

        private bool _highlighted;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            // The placeholder renderer is a child of this slot (e.g., a wireframe ring).
            // It should NOT count as an "adopted" held item. Skip it when checking for adoption.
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                // Skip the placeholder subtree. Use IsChildOf so a placeholder nested deeper than
                // a direct child is still recognized (IsChildOf returns true for self too).
                if (placeholderRenderer != null && placeholderRenderer.transform.IsChildOf(child.transform))
                    continue;
                HeldItem = child;
                break;
            }

            if (HeldItem == null && initialPrefab != null)
            {
                var inst = Instantiate(initialPrefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                HeldItem = inst;
            }
        }

        public void Place(GameObject item)
        {
            HeldItem = item;
            RefreshHighlightTarget();
        }

        public GameObject Take()
        {
            var item = HeldItem;
            HeldItem = null;
            RefreshHighlightTarget();
            return item;
        }

        public void SetHighlighted(bool on)
        {
            _highlighted = on;
            RefreshHighlightTarget();
        }

        private void RefreshHighlightTarget()
        {
            // Full slot → light up the held item. Empty slot → light up the placeholder.
            var renderers = HeldItem != null
                ? HeldItem.GetComponentsInChildren<Renderer>()
                : (placeholderRenderer != null ? new[] { placeholderRenderer } : System.Array.Empty<Renderer>());

            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mat = r.material; // instance, not shared — acceptable for P0
                if (_highlighted)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionColorID, highlightEmissive);
                }
                else
                {
                    mat.SetColor(EmissionColorID, Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
