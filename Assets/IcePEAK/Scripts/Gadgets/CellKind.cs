namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Identifies the kind of cell holding an item — used as the <c>from</c> and <c>to</c>
    /// arguments on <see cref="IHoldable.OnTransfer"/> so items can react to context
    /// (e.g., a stowed pick disables its tip collider).
    /// </summary>
    public enum CellKind
    {
        Hand,
        BeltSlot
    }
}
