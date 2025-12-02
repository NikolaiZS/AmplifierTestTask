using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTaska.Models
{
    public class StockReceipt
    {
        public int ReceiptId {  get; set; }
        public int ProductId { get; set; }
        public DateTime ReceiptDate { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
    }
}
