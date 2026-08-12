using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Users;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await userService.GetByIdAsync(userId, ct);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await userService.UpdateProfileAsync(userId, request, ct);
        return Ok(ApiResponse<UserResponse>.Ok(result, "Profile updated"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken ct)
    {
        var result = await userService.GetUsersAsync(query, ct);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPatch("{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Suspend(Guid id, CancellationToken ct)
    {
        await userService.SuspendUserAsync(id, ct);
        return Ok(ApiResponse.Ok("User suspended"));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id, CancellationToken ct)
    {
        await userService.ActivateUserAsync(id, ct);
        return Ok(ApiResponse.Ok("User activated"));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("sub")!);
}
