using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Models.Entities
{
    public class Customer : SimObject
    {
        public Customer(Guid id, double x, double y) : base(id, x, y) { }
        public CustomerState CurrentStatus { get; set; } = CustomerState.InOrderQueue;
        public WaypointPath MovementRoute { get; set; } = new();
        public int? OrderNumber { get; set; }  // Номер заказа клиента

        public string DisplayText => OrderNumber?.ToString() ?? "К";
    }
}
