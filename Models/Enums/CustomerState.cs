using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSimulation.Models.Enums
{
    public enum CustomerState
    {
        InOrderQueue,        // В очереди заказов
        Ordering,           // Делает заказ
        WaitingForOrder,    // Ждет готовый заказ
        PickingUpOrder,     // Забирает заказ
        Leaving             // Уходит
    }
}
