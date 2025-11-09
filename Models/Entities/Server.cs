using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Models.Entities
{
    public class Server : SimObject
    {
        public ServerState CurrentStatus { get; set; } = ServerState.Idle;
        public Guid? AssignedOrderId { get; set; }
        public int? AnnouncedOrderNumber { get; set; }

        public Server() : base(Guid.NewGuid(), 0, 0) { }
        public Server(Guid id, double x, double y) : base(id, x, y) { }
    }
}
