namespace Cart_worker.Model
{
    public class CartItemRequest
    {
        public Guid CartItemId { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public int Quantity { get; set; }

    }

    public class UpdateCartItemRequest
    {
        public Action Action { get; set; } = Action.UpdatePrice;
        public List<CartItemRequest> CartItems { get; set; }
    }

    public enum Action
    {
        UpdatePrice,
        Reprice
    }
}
