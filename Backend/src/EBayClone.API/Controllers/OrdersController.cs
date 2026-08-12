using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Orders;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderResponse>>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var buyerId = GetUserId();
        var result = await orderService.GetOrdersAsync(buyerId, query, ct);
        return Ok(ApiResponse<PagedResult<OrderResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var buyerId = GetUserId();
        var result = await orderService.GetByIdAsync(id, buyerId, ct);
        return Ok(ApiResponse<OrderResponse>.Ok(result));
    }

    [HttpGet("seller")]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderResponse>>>> GetSellerOrders(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var sellerId = GetUserId();
        var result = await orderService.GetSellerOrdersAsync(sellerId, query, ct);
        return Ok(ApiResponse<PagedResult<OrderResponse>>.Ok(result));
    }

    [HttpGet("seller/{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetSellerOrderById(Guid id, CancellationToken ct)
    {
        var sellerId = GetUserId();
        var result = await orderService.GetSellerOrderByIdAsync(id, sellerId, ct);
        return Ok(ApiResponse<OrderResponse>.Ok(result));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Checkout(
        [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var buyerId = GetUserId();
        var result = await orderService.CheckoutAsync(buyerId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<OrderResponse>.Ok(result, "Order placed successfully"));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var buyerId = GetUserId();
        await orderService.CancelAsync(id, buyerId, ct);
        return Ok(ApiResponse.Ok("Order cancelled"));
    }

    [HttpPut("seller/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> SellerUpdateStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var sellerId = GetUserId();
        var result = await orderService.SellerShipAsync(id, sellerId, request, ct);
        return Ok(ApiResponse<OrderResponse>.Ok(result, "Order updated"));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("sub")!);
}
