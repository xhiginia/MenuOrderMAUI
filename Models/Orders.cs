using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOrderMAUI.Models
{
    public class Orders
    {
        public int OrderID { get; set; }
        public int ItemID { get; set; }
        public required string ItemName { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
