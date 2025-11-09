using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Engine
{
    public class CookStateEventArgs : EventArgs
    {
        public Guid CookId { get; init; }
        public CookState State { get; init; }
        public CookStateEventArgs(Guid cookId, CookState state) { CookId = cookId; State = state; }
    }
}
