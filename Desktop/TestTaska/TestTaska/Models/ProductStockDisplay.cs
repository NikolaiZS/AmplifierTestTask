using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTaska.Models
{
    public class ProductStockDisplay
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        public int TotalIn {  get; set; }
        public int TotalOut { get; set; }
        public int CurrentStock => TotalIn - TotalOut;
    }
}
