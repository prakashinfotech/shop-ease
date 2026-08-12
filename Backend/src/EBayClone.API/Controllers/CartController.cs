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
public class CartController(IOrderService orderService) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Checkout(
        [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var buyerId = Guid.Parse(User.FindFirstValue("sub")!);
        var result = await orderService.CheckoutAsync(buyerId, request, ct);
        return Ok(ApiResponse<OrderResponse>.Ok(result, "Order placed successfully"));
    }
}
