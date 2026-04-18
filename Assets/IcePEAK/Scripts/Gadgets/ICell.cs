using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// A single-item container — either a hand or a belt slot.
    /// Held items are parented to Anchor.
    /// </summary>
    public interface ICell
    {
        GameObject HeldItem { get; }
        Transform Anchor { get; }
        CellKind Kind { get; }

        /// Register <paramref name="item"/> as this cell's content. Caller parents the transform.
        void Place(GameObject item);

        /// Returns the current HeldItem (or null) and clears the cell. Does not reparent.
        GameObject Take();
    }
}
