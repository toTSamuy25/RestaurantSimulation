using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSimulation.Models.Entities
{
    public class CustomerOrderEventArgs : EventArgs
    {
        public Guid CustomerId { get; init; }
        public int? OrderNumber { get; init; }

        public CustomerOrderEventArgs(Guid customerId, int? orderNumber)
        {
            CustomerId = customerId;
            OrderNumber = orderNumber;
        }
    }
}
