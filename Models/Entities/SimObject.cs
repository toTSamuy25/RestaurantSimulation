namespace RestaurantSimulation.Models.Entities
{
    public class SimObject
    {
        public Guid Id { get; }      // Уникальный идентификатор
        public double X { get; set; } // Координата X на canvas
        public double Y { get; set; } // Координата Y на canvas

        public SimObject(Guid id, double x, double y)
        {
            Id = id; X = x; Y = y;
        }
    }
}