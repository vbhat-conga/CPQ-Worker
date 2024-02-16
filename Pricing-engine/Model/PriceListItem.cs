using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricing_Engine.Model
{
    public class PriceListItemData
    {
        public Guid PriceListItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid PriceListId { get; set; }
        public string? Name { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } 
        public string ModifiedBy { get; set; } 
        public DateTime ModifiedDate { get; set; } 
        public string? ExternalId { get; set; }
        public bool AutoRenew { get; set; } 
        public double? AutoRenewalTerm { get; set; }
        public AutoRenewalType AutoRenewalType { get; set; } 
        public BillingFrequency BillingFrequency { get; set; } 
        public BillingRule BillingRule { get; set; } 
        public ChargeType ChargeType { get; set; } 
        public int DefaultQuantity { get; set; } 
        public string? Description { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Currency { get; set; }
        public double Price { get; set; } 
    }
}
