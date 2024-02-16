using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart_worker.Model
{
    internal class UpdateCartRequest
    {
        public Guid CartId { get; set; }
        public CartStatus Status { get; set; }
        public double Price { get; set; }

        public Guid PriceListId { get; set; }

    }

    public enum CartStatus
    {
        Unknown = 0,
        Created = 1,
        Conigured = 2,
        Priced = 3
    }
}
