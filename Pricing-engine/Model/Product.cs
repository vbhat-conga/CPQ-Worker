using System.ComponentModel.DataAnnotations;

namespace Pricing_Engine.Model
{
    public class ProductData
    {
        public ConfigurationTypeEnum ConfigurationType { get; set; }
        public string? Description { get; set; } 
        public DateTime? EffectiveDate { get; set; } 
        public string? DisplayUrl { get; set; } 
        public bool ExcludeFromSitemap { get; set; } 
        public DateTime? ExpirationDate { get; set; } 
        public FamilyEnum Family { get; set; } 
        public bool HasAttributes { get; set; }
        public bool HasDefaults { get; set; } 
        public bool HasOptions { get; set; } 
        public bool HasSearchAttributes { get; set; } 
        public bool IsPlainProduct { get; set; } 
        public string? ImageURL { get; set; } 
        public bool IsActive { get; set; } 
        public bool IsCustomizable { get; set; }
        public bool IsTabViewEnabled { get; set; } 
        public string ProductCode { get; set; } 
        public ProductTypeEnum ProductType { get; set; } 
        public QuantityUOM QuantityUnitOfMeasure { get; set; } 
        public double? RenewalLeadTime { get; set; } 
        public string? StockKeepingUnit { get; set; } 
        public UOMEnum Uom { get; set; } 
        public double Version { get; set; }
        [Required]
        public Guid ProductId { get; set; }
        public string? Name { get; set; }
        public string CreatedBy { get; set; } 
        public DateTime CreatedDate { get; set; } 
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; } 
        public string? ExternalId { get; set; } 
        public double Price { get; set; } 
        public bool AutoRenew { get; set; } 
        public double? AutoRenewalTerm { get; set; }
        public AutoRenewalType AutoRenewalType { get; set; } 
        public BillingFrequency BillingFrequency { get; set; } 
        public BillingRule BillingRule { get; set; } 
        public ChargeType ChargeType { get; set; }
        public int DefaultQuantity { get; set; }
        public string Currency { get; set; } 
    }

    public enum ProductTypeEnum
    {
        Equipment = 1,
        Service = 2,
        Entitlement = 3,
        License = 4,
        Maintenance = 5,
        Wallet = 6,
        Subscription = 7,
        ProfessionalServices = 8,
        Solution = 9

    }
    public enum FamilyEnum
    {
        Software = 1,
        Hardware = 2,
        MaintenanceHW = 3,
        Implementation = 4,
        Training = 5,
        Other = 6,
        MaintenanceSW = 7
    }
    public enum QuantityUOM
    {
        Each = 1
    }
    public enum UOMEnum
    {
        Each = 1,
        Hour = 2,
        Day = 3,
        Month = 4,
        Year = 5,
        Quarter = 6,
        Case = 7,
        Gallon = 8
    }
    public enum ConfigurationTypeEnum
    {
        Standalone = 1,
        Bundle = 2,
        Option = 3
    }


    public enum AutoRenewalType
    {
        Fixed,
        Evergreen,
        DoNotRenew
    }

    public enum BillingRule
    {
        BillInAdvance,
        BillInArrears,
        MilestoneBilling
    }

    public enum BillingFrequency
    {
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        HalfYearly,
        Yearly,
        OneTime
    }


    public enum ChargeType
    {
        StandardPrice,
        LicenseFee,
        SubscriptionFee,
        ImplementationFee,
        InstallationFee,
        MaintenanceFee,
        Adjustment,
        ServiceFee,
        RentalPrice,
        SalesPrice,
        UsageFee
    }
}
