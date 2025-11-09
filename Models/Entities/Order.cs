using RestaurantSimulation.Models.Enums;

namespace RestaurantSimulation.Models.Entities
{
    public class Order
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int OrderNumber { get; set; }  // Номер заказа для клиента
        public OrderState CurrentStatus { get; set; } = OrderState.New;
        public double WillBeCompletedAtTime { get; set; } = 0;
        public Guid CustomerId { get; set; }
        public Guid OrderTakerId { get; set; }
        public Guid? ServerId { get; set; }
        public Guid? AssignedChefId { get; set; } // Добавляем назначенного повара
        public Guid OrderTokenId { get; set; } = Guid.NewGuid();
        public Guid DishTokenId { get; set; } = Guid.NewGuid();
    }
}
