using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Models.Entities
{
    public class OrderTaker : SimObject
    {
        public OrderTakerState CurrentStatus { get; set; } = OrderTakerState.Idle;
        public WaypointPath MovementRoute { get; set; } = new();
        public Guid? AssignedCustomerId { get; set; }
        public Guid? AssignedOrderId { get; set; }
        public int? AssignedOrderNumber { get; set; }
        public double WillBeFreeAtTime { get; set; } = 0; // ƒобавл€ем врем€ завершени€ приема заказа


        public OrderTaker() : base(Guid.NewGuid(), 0, 0) { }
        public OrderTaker(Guid id, double x, double y) : base(id, x, y) { }
    }
}
