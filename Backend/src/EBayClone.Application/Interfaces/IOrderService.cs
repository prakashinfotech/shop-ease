using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Orders;

namespace EBayClone.Application.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderResponse>> GetOrdersAsync(Guid buyerId, PagedQuery query, CancellationToken ct = default);
    Task<OrderResponse> GetByIdAsync(Guid id, Guid buyerId, CancellationToken ct = default);
    Task<PagedResult<OrderResponse>> GetSellerOrdersAsync(Guid sellerId, PagedQuery query, CancellationToken ct = default);
    Task<OrderResponse> GetSellerOrderByIdAsync(Guid id, Guid sellerId, CancellationToken ct = default);
    Task<OrderResponse> CheckoutAsync(Guid buyerId, CheckoutRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid id, Guid buyerId, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<OrderResponse> SellerShipAsync(Guid id, Guid sellerId, UpdateOrderStatusRequest request, CancellationToken ct = default);
}
