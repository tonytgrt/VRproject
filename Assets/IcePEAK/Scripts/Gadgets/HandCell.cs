using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// "Hand holds one item" cell. Attach to a child GameObject of the hand controller.
    /// The GameObject's own transform is the anchor — held items are parented to it by the caller.
    /// </summary>
    public class HandCell : MonoBehaviour, ICell
    {
        [Tooltip("If the anchor has no existing child at Awake, instantiate this prefab into it. Optional.")]
        [SerializeField] private GameObject initialPrefab;

        public GameObject HeldItem { get; private set; }
        public Transform Anchor => transform;
        public CellKind Kind => CellKind.Hand;

        private void Awake()
        {
            // Rule 1: adopt first existing child (e.g., IcePickLeft reparented under us in the scene).
            if (transform.childCount > 0)
            {
                HeldItem = transform.GetChild(0).gameObject;
                return;
            }

            // Rule 2: instantiate initialPrefab if provided.
            if (initialPrefab != null)
            {
                var inst = Instantiate(initialPrefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                HeldItem = inst;
            }
        }

        public void Place(GameObject item) { HeldItem = item; }

        public GameObject Take()
        {
            var item = HeldItem;
            HeldItem = null;
            return item;
        }
    }
}
