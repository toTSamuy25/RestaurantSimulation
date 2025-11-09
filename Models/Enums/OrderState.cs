namespace RestaurantSimulation.Models.Enums
{
    public enum OrderState
    {
        New,                // Новый заказ
        InKitchenQueue,     // В очереди кухни
        Cooking,            // Готовится
        Ready,              // Готов к выдаче
        Announced,          // Объявлен сервером
        PickedUp,           // Забран клиентом
        Completed           // Завершен
    }
}
