using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTaska.Models
{
    public class StockOut
    {
        public int OutId { get; set; }
        public int ProductId { get; set; }
        public DateTime OutDate { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
    }
}
