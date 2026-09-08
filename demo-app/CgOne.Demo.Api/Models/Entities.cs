namespace CgOne.Demo.Api.Models;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "NEW";
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class InventoryItem
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class PaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CardNumber { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Accepted { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int TransactionId { get; set; }
}

public class OrderRequest
{
    public int UserId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
