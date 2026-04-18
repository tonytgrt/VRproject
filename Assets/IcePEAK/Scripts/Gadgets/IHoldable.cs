namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Implemented by any item that can live in a HandCell or BeltSlot.
    /// Called once per transfer, after reparenting is complete.
    /// </summary>
    public interface IHoldable
    {
        void OnTransfer(CellKind from, CellKind to);
    }
}
