using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSimulation.Engine
{
    public class SimulationConfig
    {
        public int CustomersPerMinute { get; set; }
        public int OrderTakers { get; set; }
        public int Chefs { get; set; }
        public int Servers { get; set; }
        public int CookingTime { get; set; }
        public int OrderTakingTime { get; set; }

        // Скорость движения клиентов/официантов
        public double CustomersSpeed { get; set; }

        // Множитель для анимации (зависит от скорости)
        public double AnimationSpeedMultiplier =>
            CustomersSpeed > 0 ? 1.0 / CustomersSpeed : 1.0;
    }
}

