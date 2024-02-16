using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricing_Engine.Model
{
    public class LineItemQueryRequest
    {
        [Required]
        public IEnumerable<Guid> Ids { get; set; }

        public IEnumerable<string> Fields { get; set; }
    }
}
