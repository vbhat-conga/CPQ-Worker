using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Pricing_Engine.Model
{
    public class BaseMessage
    {
        public Dictionary<string, byte[]> AdditonalInfo { get; set; }

        public BaseMessage()
        {
            AdditonalInfo = new();
        }
    }
    public class CartMessage : BaseMessage
    {
        public Guid CartId { get; set; }
        public IEnumerable<CartItemInfo> CartItems { get; set; }
        public Guid PriceListId { get; set; }
        public CartAction CartAction { get; set; } = CartAction.ConfigureAndPrice;

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CartAction
    {
        ConfigureAndPrice,
        Price,
        Reprice
    }
    public class CartItemInfo
    {
        public Guid CartItemId { get; set; }
        public bool IsPrimaryLine { get; set; }
        public LineType LineType { get; set; }
        public int Quantity { get; set; }
        public string? ExternalId { get; set; }
        public int PrimaryTaxLineNumber { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; } = "USD";
        public Guid ProductId { get; set; }

    }


    public enum LineType
    {
        None = 0,
        ProductService = 1
    }
}

