namespace RestaurantSimulation.Models.Enums
{
    public enum OrderTakerState
    {
        Idle,                   // Свободен - ждет клиента
        TakingOrder,            // Принимает заказ - общается с клиентом
        ProcessingOrder,        // Обрабатывает заказ - несет заказ на кухню
        ReturningToOrderArea    // Возвращается - идет обратно на рабочее место
    }
}
