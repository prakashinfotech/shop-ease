using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Users;

namespace EBayClone.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetUsersAsync(PagedQuery query, CancellationToken ct = default);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct = default);
    Task SuspendUserAsync(Guid id, CancellationToken ct = default);
    Task ActivateUserAsync(Guid id, CancellationToken ct = default);
}
