using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Models.Entities
{
    public class Chef
    {
        public Guid Id { get; } = Guid.NewGuid();
        public CookState CurrentStatus { get; set; } = CookState.Idle;
        public double WillBeFreeAtTime { get; set; } = 0;
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
    }
}
