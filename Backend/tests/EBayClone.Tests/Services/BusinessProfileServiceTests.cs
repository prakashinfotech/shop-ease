using System.Linq.Expressions;
using Xunit;
using EBayClone.Application.DTOs.BusinessProfile;
using EBayClone.Application.Interfaces;
using EBayClone.Application.Services;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using EBayClone.Tests.Helpers;
using Moq;

namespace EBayClone.Tests.Services;

public class BusinessProfileServiceTests
{
    private readonly Mock<IRepository<BusinessProfile>> _profileRepo = new();
    private readonly Mock<IRepository<UserDocument>> _docRepo = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<IBackgroundEmailService> _bgEmail = new();
    private readonly Mock<ILogger<BusinessProfileService>> _logger = new();

    private BusinessProfileService CreateSut() => new(
        _profileRepo.Object, _docRepo.Object, _userRepo.Object,
        _fileStorage.Object, _bgEmail.Object, _logger.Object);

    private static BusinessProfileRequest SampleRequest() =>
        new("Acme Ltd", "GST123456789", "PAN1234567", "123 St");

    [Fact]
    public async Task SubmitAsync_GstAlreadyTaken_ThrowsInvalidOperationException()
    {
        _profileRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessProfile?)null);

        _profileRepo.SetupSequence(r => r.ExistsAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitAsync(Guid.NewGuid(), SampleRequest()));
    }

    [Fact]
    public async Task SubmitAsync_PanAlreadyTaken_ThrowsInvalidOperationException()
    {
        _profileRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessProfile?)null);

        _profileRepo.SetupSequence(r => r.ExistsAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitAsync(Guid.NewGuid(), SampleRequest()));
    }

    [Fact]
    public async Task UpdateAsync_GstAlreadyTaken_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var profile = new BusinessProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VerificationStatus = VerificationStatus.Draft
        };

        var profiles = new List<BusinessProfile> { profile }.AsAsyncQueryable();
        _profileRepo.Setup(r => r.Query()).Returns(profiles);

        _profileRepo.SetupSequence(r => r.ExistsAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAsync(userId, SampleRequest()));
    }

    [Fact]
    public async Task UpdateAsync_PanAlreadyTaken_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var profile = new BusinessProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VerificationStatus = VerificationStatus.Draft
        };

        var profiles = new List<BusinessProfile> { profile }.AsAsyncQueryable();
        _profileRepo.Setup(r => r.Query()).Returns(profiles);

        _profileRepo.SetupSequence(r => r.ExistsAsync(
                It.IsAny<Expression<Func<BusinessProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAsync(userId, SampleRequest()));
    }
}
