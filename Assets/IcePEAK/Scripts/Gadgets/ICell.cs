using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// A single-item container — either a hand or a belt slot.
    /// Held items are expected to be parented to <see cref="Anchor"/> by the caller.
    /// </summary>
    public interface ICell
    {
        GameObject HeldItem { get; }
        Transform Anchor { get; }
        CellKind Kind { get; }

        /// <summary>
        /// Register <paramref name="item"/> as this cell's content.
        /// Caller is responsible for setting <c>item.transform.parent = Anchor</c>.
        /// </summary>
        void Place(GameObject item);

        /// <summary>
        /// Returns the current <see cref="HeldItem"/> (or null) and clears the cell.
        /// Safe to call when empty; returns null. Implementations MUST NOT touch <c>item.transform.parent</c>.
        /// </summary>
        GameObject Take();
    }
}
