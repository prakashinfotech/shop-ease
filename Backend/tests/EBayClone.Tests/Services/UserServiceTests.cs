using System.Linq.Expressions;
using Xunit;
using EBayClone.Application.DTOs.Users;
using EBayClone.Application.Services;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Interfaces;
using EBayClone.Tests.Helpers;
using Moq;

namespace EBayClone.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _userRepo = new();

    private UserService CreateSut() => new(_userRepo.Object);

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        var users = new List<User>().AsAsyncQueryable();
        _userRepo.Setup(r => r.Query()).Returns(users);

        var sut = CreateSut();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequest("A", "B", "a@b.com")));
    }

    [Fact]
    public async Task UpdateProfileAsync_EmailAlreadyTaken_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { Id = userId, Email = "old@example.com" }
        }.AsAsyncQueryable();

        _userRepo.Setup(r => r.Query()).Returns(users);
        _userRepo.Setup(r => r.ExistsAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateProfileAsync(userId, new UpdateProfileRequest("A", "B", "TAKEN@EXAMPLE.COM")));
    }
}
