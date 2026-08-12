namespace EBayClone.Application.DTOs.Orders;

public record OrderResponse(
    Guid Id,
    string OrderNumber,
    decimal TotalAmount,
    int Status,
    string PaymentMethod,
    int PaymentStatus,
    string? PaymentReference,
    string? UpiId,
    string? ShippingAddress,
    string? Carrier,
    string? TrackingNumber,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    string? Notes,
    Guid BuyerId,
    string BuyerName,
    IEnumerable<OrderItemResponse> Items,
    int ItemCount,
    DateTime CreatedAt
);

public record OrderItemResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    int Quantity,
    decimal UnitPrice
);
