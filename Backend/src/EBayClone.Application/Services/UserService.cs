using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Users;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Application.Services;

public class UserService(IRepository<User> userRepository) : IUserService
{
    private const string UserNotFound = "User not found.";
    public async Task<PagedResult<UserResponse>> GetUsersAsync(PagedQuery query, CancellationToken ct = default)
    {
        var q = userRepository.Query().IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(u =>
                u.FirstName.Contains(query.Search) ||
                u.LastName.Contains(query.Search) ||
                u.Email.Contains(query.Search));

        q = q.OrderByDescending(u => u.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<UserResponse>.Create(
            items.Select(MapToResponse).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException(UserNotFound);
        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException(UserNotFound);

        var emailLower = request.Email.ToLower();
        var emailTaken = await userRepository.ExistsAsync(
            u => u.Email == emailLower && u.Id != id, ct);

        if (emailTaken)
            throw new InvalidOperationException("Email is already in use.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.ToLower().Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return MapToResponse(user);
    }

    public async Task SuspendUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException(UserNotFound);

        user.IsSuspended = true;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task ActivateUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException(UserNotFound);

        user.IsSuspended = false;
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    private static UserResponse MapToResponse(User u) => new(
        u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.AvatarUrl,
        (int)u.AccountType, u.Role.ToString(), u.IsEmailVerified, u.IsSuspended, u.IsDeleted, u.CreatedAt,
        u.BusinessProfile?.VerificationStatus.ToString()
    );
}
