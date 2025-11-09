using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSimulation.Engine
{
    public class SimulationConfig
    {
        public int CustomersPerMinute { get; set; } = 10;
        public int OrderTakers { get; set; } = 1;
        public int Chefs { get; set; } = 2;
        public int Servers { get; set; } = 1;
        public int CookingTime { get; set; } = 5;
        public int OrderTakingTime { get; set; } = 3; // Добавляем время принятия заказа

        public double CustomersSpeed { get; set; } = 1;
        public double AnimationSpeedMultiplier => 1.0 / CustomersSpeed;

    }
}
