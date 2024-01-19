using System.ComponentModel.DataAnnotations;

namespace Pricing_Engine.Model
{
    public class ProductData
    {
        [Required]
        public ConfigurationTypeEnum ConfigurationType { get; set; }
        public string? Description { get; set; } = null;
        public DateTime? EffectiveDate { get; set; } = null;
        public string? DisplayUrl { get; set; } = null;
        public bool ExcludeFromSitemap { get; set; } = false;
        public DateTime? ExpirationDate { get; set; } = null;
        public FamilyEnum Family { get; set; } = FamilyEnum.Software;
        public bool HasAttributes { get; set; } = false;
        public bool HasDefaults { get; set; } = false;
        public bool HasOptions { get; set; } = false;
        public bool HasSearchAttributes { get; set; } = false;
        public bool IsPlainProduct { get; set; } = true;
        public string? ImageURL { get; set; } = null;
        public bool IsActive { get; set; } = true;
        public bool IsCustomizable { get; set; } = false;
        public bool IsTabViewEnabled { get; set; } = false;
        public string ProductCode { get; set; } = "std";
        public ProductTypeEnum ProductType { get; set; } = ProductTypeEnum.Service;
        public QuantityUOM QuantityUnitOfMeasure { get; set; } = QuantityUOM.Each;
        public double? RenewalLeadTime { get; set; } = null;
        public string? StockKeepingUnit { get; set; } = null;
        public UOMEnum Uom { get; set; } = UOMEnum.Each;
        public double Version { get; set; } = 1.0;
        [Required]
        public Guid ProductId { get; set; }
        public string? Name { get; set; }
        public string CreatedBy { get; set; } = "Admin";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = "Admin";
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public string? ExternalId { get; set; } = null;
        public double Price { get; set; } = 0.0;
        public bool AutoRenew { get; set; } = false;
        public double? AutoRenewalTerm { get; set; } = null;
        public AutoRenewalType AutoRenewalType { get; set; } = AutoRenewalType.DoNotRenew;
        public BillingFrequency BillingFrequency { get; set; } = BillingFrequency.Yearly;
        public BillingRule BillingRule { get; set; } = BillingRule.BillInAdvance;
        public ChargeType ChargeType { get; set; } = ChargeType.StandardPrice;
        public int DefaultQuantity { get; set; } = 1;
        public string Currency { get; set; } = "USD";
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
