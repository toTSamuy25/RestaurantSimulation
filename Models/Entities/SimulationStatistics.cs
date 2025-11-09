namespace RestaurantSimulation.Models.Entities
{
    public class SimulationStatistics
    {
        public int OrderQueueCount { get; set; } = 0;        // В очереди заказов
        public List<int> CurrentCookingOrders { get; set; } = new();  // Готовящиеся заказы
        public int WaitingOrdersCount { get; set; } = 0;     // Ожидает готовки
        public List<int> ReadyOrderNumbers { get; set; } = new(); // Готовые к выдаче заказы
        public int PickupQueueCount { get; set; } = 0;       // В очереди выдачи
    }
}
