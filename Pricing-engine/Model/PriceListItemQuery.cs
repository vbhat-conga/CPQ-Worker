using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricing_Engine.Model
{
    internal class PriceListItemQuery
    {
            public IEnumerable<Guid> Ids { get; set; }
            public IEnumerable<string> QueryFields { get; set; }        
    }
}
