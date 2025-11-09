using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSimulation.Models.Entities
{
    public class WaypointPath
    {
        public List<(double X, double Y)> Points { get; } = new(); // Список точек маршрута
        public int Index { get; private set; } = 0;               // Текущая точка
        public bool IsDone => Index >= Points.Count;              // Маршрут завершен?
        public (double X, double Y)? Current => IsDone ? null : Points[Index]; // Текущая цель
        public void Reset() => Index = 0;
        public void Advance() => Index++;
        public WaypointPath Clone()
        {
            var p = new WaypointPath();
            p.Points.AddRange(Points);
            return p;
        }
    }
}
