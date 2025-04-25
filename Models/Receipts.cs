using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOrderMAUI.Models
{
    public class Receipts
    {
        public int ReceiptID { get; set; }
        public int OrderID { get; set; }
        public decimal TotalBill { get; set; }
        public DateTime GenerateDate { get; set; }
    }
}
