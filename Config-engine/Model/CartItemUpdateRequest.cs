using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart_Worker.Model
{
    public class CartItemUpdateRequest
    {
        public Guid CartItemId { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
    }
}
