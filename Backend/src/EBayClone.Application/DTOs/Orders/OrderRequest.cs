namespace EBayClone.Application.DTOs.Orders;

public record CheckoutRequest(
    IEnumerable<CheckoutItem> Items,
    string? ShippingAddress = null,
    string? Notes = null,
    string PaymentMethod = "UPI",
    string? UpiId = null
);

public record CheckoutItem(Guid ListingId, int Quantity);

public record UpdateOrderStatusRequest(
    int Status,
    string? Carrier = null,
    string? TrackingNumber = null
);
