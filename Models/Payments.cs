using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOrderMAUI.Models
{
    public class Payments
    {
        public int PaymentID { get; set; }
        public int OrderID { get; set; }
        public string PaymentMethod { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal TotalBill {  get; set; }
        public decimal ChangeAmount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
