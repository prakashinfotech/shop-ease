using System.Linq.Expressions;
using Xunit;
using EBayClone.Application.DTOs.Auth;
using EBayClone.Application.Interfaces;
using EBayClone.Application.Services;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using EBayClone.Tests.Helpers;
using Moq;

namespace EBayClone.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IBackgroundEmailService> _bgEmail = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();

    private AuthService CreateSut() => new(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _jwtService.Object,
        _passwordHasher.Object,
        _bgEmail.Object,
        _logger.Object);

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        _userRepo.Setup(r => r.ExistsAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var request = new RegisterRequest("Alice", "Smith", "ALICE@EXAMPLE.COM", "pass123");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        var users = new List<User>().AsAsyncQueryable();
        _userRepo.Setup(r => r.Query()).Returns(users);

        var sut = CreateSut();
        var request = new LoginRequest("NOTFOUND@EXAMPLE.COM", "pass");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { Id = userId, Email = "user@example.com", PasswordHash = "hash" }
        }.AsAsyncQueryable();

        _userRepo.Setup(r => r.Query()).Returns(users);
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var sut = CreateSut();
        var request = new LoginRequest("USER@EXAMPLE.COM", "wrong");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(request));
    }

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_CompletesWithoutError()
    {
        _userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();
        var ex = await Record.ExceptionAsync(() =>
            sut.ForgotPasswordAsync(new ForgotPasswordRequest("UNKNOWN@EXAMPLE.COM")));

        Assert.Null(ex);
    }
}
