using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart_worker.Model
{
    internal class CartResponse
    {
        public Guid CartId { get; set; }
        public string Name { get; set; }
        public Guid PriceListId { get; set; }
        public string Status { get; set; }
        public double Price { get; set; }
    }


}
