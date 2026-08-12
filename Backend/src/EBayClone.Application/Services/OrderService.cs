using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Orders;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<Listing> listingRepository,
    IRepository<User> userRepository,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<PagedResult<OrderResponse>> GetOrdersAsync(Guid buyerId, PagedQuery query, CancellationToken ct = default)
    {
        var q = orderRepository.Query()
            .Include(o => o.Items)
                .ThenInclude(i => i.Listing)
            .Include(o => o.Buyer)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<OrderResponse>.Create(
            items.Select(o => MapToResponse(o)).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, Guid buyerId, CancellationToken ct = default)
    {
        var order = await orderRepository.Query()
            .Include(o => o.Items)
                .ThenInclude(i => i.Listing)
            .Include(o => o.Buyer)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        if (order.BuyerId != buyerId)
            throw new UnauthorizedAccessException("Access denied.");

        return MapToResponse(order);
    }

    public async Task<PagedResult<OrderResponse>> GetSellerOrdersAsync(Guid sellerId, PagedQuery query, CancellationToken ct = default)
    {
        var q = orderRepository.Query()
            .Include(o => o.Items)
            .Include(o => o.Buyer)
            .Where(o => o.Items.Any(i => i.Listing.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<OrderResponse>.Create(
            items.Select(o => MapToResponse(o, sellerId)).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<OrderResponse> GetSellerOrderByIdAsync(Guid id, Guid sellerId, CancellationToken ct = default)
    {
        var order = await orderRepository.Query()
            .Include(o => o.Items)
                .ThenInclude(i => i.Listing)
            .Include(o => o.Buyer)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.Items.Any(i => i.Listing.SellerId == sellerId), ct)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        return MapToResponse(order, sellerId);
    }

    public async Task<OrderResponse> CheckoutAsync(Guid buyerId, CheckoutRequest request, CancellationToken ct = default)
    {
        var buyer = await userRepository.GetByIdAsync(buyerId, ct)
            ?? throw new KeyNotFoundException("Buyer not found.");

        var checkoutItems = request.Items?.ToList() ?? [];
        if (checkoutItems.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        if (checkoutItems.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var listingIds = checkoutItems.Select(i => i.ListingId).Distinct().ToList();
        var listings = await listingRepository.Query()
            .Where(l => listingIds.Contains(l.Id) && l.Status == ListingStatus.Active)
            .ToListAsync(ct);

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        var isUpi = paymentMethod == "UPI";
        if (isUpi && string.IsNullOrWhiteSpace(request.UpiId))
            throw new InvalidOperationException("UPI ID is required.");

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            BuyerId = buyerId,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            PaymentMethod = paymentMethod,
            PaymentStatus = isUpi ? PaymentStatus.Paid : PaymentStatus.Pending,
            PaymentReference = isUpi ? GeneratePaymentReference() : null,
            UpiId = isUpi ? request.UpiId?.Trim() : null,
            Status = isUpi ? OrderStatus.Confirmed : OrderStatus.Pending,
        };

        foreach (var item in checkoutItems)
        {
            var listing = listings.FirstOrDefault(l => l.Id == item.ListingId)
                ?? throw new InvalidOperationException($"Listing {item.ListingId} is not available.");

            if (listing.SellerId == buyerId)
                throw new InvalidOperationException("You cannot buy your own listing.");

            if (listing.Quantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for '{listing.Title}'.");

            order.Items.Add(new OrderItem
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                Quantity = item.Quantity,
                UnitPrice = GetFinalPrice(listing),
            });

            listing.Quantity -= item.Quantity;
            if (listing.Quantity == 0)
                listing.Status = ListingStatus.Sold;

            listingRepository.Update(listing);
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        await orderRepository.AddAsync(order, ct);
        await orderRepository.SaveChangesAsync(ct);

        logger.LogInformation("Order created: {OrderNumber} for buyer {BuyerId}", order.OrderNumber, buyerId);
        logger.LogInformation("[EMAIL] Order Placed | To: {Email} | Order: {OrderNumber}", buyer.Email, order.OrderNumber);

        order.Buyer = buyer;
        return MapToResponse(order);
    }

    public async Task CancelAsync(Guid id, Guid buyerId, CancellationToken ct = default)
    {
        var order = await orderRepository.Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        if (order.BuyerId != buyerId)
            throw new UnauthorizedAccessException("Access denied.");

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new InvalidOperationException("Only pending or confirmed orders can be cancelled.");

        await RestoreStockAsync(order, ct);

        order.Status = OrderStatus.Cancelled;
        if (order.PaymentStatus == PaymentStatus.Paid)
            order.PaymentStatus = PaymentStatus.Refunded;
        order.UpdatedAt = DateTime.UtcNow;
        orderRepository.Update(order);
        await orderRepository.SaveChangesAsync(ct);
        logger.LogInformation("[EMAIL] Order Cancelled | Order: {OrderNumber}", order.OrderNumber);
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await orderRepository.Query()
            .Include(o => o.Items)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        if (request.Status is (int)OrderStatus.Cancelled or (int)OrderStatus.Refunded)
            await RestoreStockAsync(order, ct);

        ApplyStatusUpdate(order, request);
        await orderRepository.SaveChangesAsync(ct);
        logger.LogInformation("[EMAIL] Order Status Updated | To: {Email} | Order: {OrderNumber} | Status: {Status}",
            order.Buyer.Email, order.OrderNumber, order.Status);
        return MapToResponse(order);
    }

    public async Task<OrderResponse> SellerShipAsync(Guid id, Guid sellerId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await orderRepository.Query()
            .Include(o => o.Items)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == id && o.Items.Any(i => i.Listing.SellerId == sellerId), ct)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        if (request.Status is not ((int)OrderStatus.Shipped or (int)OrderStatus.Delivered))
            throw new InvalidOperationException("Sellers can only mark orders as shipped or delivered.");

        ApplyStatusUpdate(order, request);
        await orderRepository.SaveChangesAsync(ct);
        logger.LogInformation("[EMAIL] Seller Order Status Updated | To: {Email} | Order: {OrderNumber} | Status: {Status}",
            order.Buyer.Email, order.OrderNumber, order.Status);
        return MapToResponse(order, sellerId);
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

    private static string GeneratePaymentReference() =>
        $"UPI-DEMO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static decimal GetFinalPrice(Listing listing) =>
        Math.Max(listing.Price - listing.DiscountAmount, 0);

    private static string NormalizePaymentMethod(string? paymentMethod) =>
        string.Equals(paymentMethod, "UPI", StringComparison.OrdinalIgnoreCase) ? "UPI" : "COD";

    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var listing = await listingRepository.GetByIdAsync(item.ListingId, ct);
            if (listing is null) continue;
            listing.Quantity += item.Quantity;
            if (listing.Status == ListingStatus.Sold)
                listing.Status = ListingStatus.Active;
            listing.UpdatedAt = DateTime.UtcNow;
            listingRepository.Update(listing);
        }
    }

    private static void ApplyStatusUpdate(Order order, UpdateOrderStatusRequest request)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
            throw new InvalidOperationException("Invalid order status.");

        var nextStatus = (OrderStatus)request.Status;
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new InvalidOperationException("Cancelled or refunded orders cannot be updated.");

        if (nextStatus == OrderStatus.Shipped)
        {
            if (string.IsNullOrWhiteSpace(request.Carrier) || string.IsNullOrWhiteSpace(request.TrackingNumber))
                throw new InvalidOperationException("Carrier and tracking number are required to ship an order.");
            order.Carrier = request.Carrier.Trim();
            order.TrackingNumber = request.TrackingNumber.Trim();
            order.ShippedAt ??= DateTime.UtcNow;
        }

        if (nextStatus == OrderStatus.Delivered)
        {
            if (order.Status != OrderStatus.Shipped)
                throw new InvalidOperationException("Only shipped orders can be marked delivered.");
            order.DeliveredAt ??= DateTime.UtcNow;
        }

        if (nextStatus == OrderStatus.Confirmed && order.PaymentStatus == PaymentStatus.Pending)
            order.PaymentStatus = PaymentStatus.Paid;

        if (nextStatus == OrderStatus.Refunded)
            order.PaymentStatus = PaymentStatus.Refunded;

        order.Status = nextStatus;
        order.UpdatedAt = DateTime.UtcNow;
    }

    private static OrderResponse MapToResponse(Order o, Guid? sellerId = null)
    {
        var items = sellerId.HasValue
            ? o.Items.Where(i => i.Listing?.SellerId == sellerId.Value)
            : o.Items;

        return new OrderResponse(
        o.Id, o.OrderNumber, o.TotalAmount, (int)o.Status, o.PaymentMethod, (int)o.PaymentStatus,
        o.PaymentReference, o.UpiId, o.ShippingAddress, o.Carrier, o.TrackingNumber, o.ShippedAt, o.DeliveredAt, o.Notes,
        o.BuyerId, $"{o.Buyer?.FirstName} {o.Buyer?.LastName}".Trim(),
        items.Select(i => new OrderItemResponse(i.Id, i.ListingId, i.ListingTitle, i.Quantity, i.UnitPrice)),
        items.Count(), o.CreatedAt
    );
    }
}
