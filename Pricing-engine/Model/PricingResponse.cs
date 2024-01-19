using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricing_Engine.Model
{
    internal class PricingResponse
    {
        public PricingResponse(Guid cartId)
        {
            CartId=cartId;
            CartItems = new();
            TotalPrice = 0;
        }
        public Guid CartId { get; set; }
        public Guid PriceListId { get; set; }
        public List<CartItem> CartItems { get; set; }
        public double TotalPrice { get; set; }

    }

    internal class CartItem
    {
        public Guid CartItemId { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
    }
}
