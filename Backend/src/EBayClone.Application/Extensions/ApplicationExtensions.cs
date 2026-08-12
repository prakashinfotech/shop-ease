using EBayClone.Application.Interfaces;
using EBayClone.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EBayClone.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IListingApprovalService, ListingApprovalService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddSingleton<AuctionBidLockService>();

        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }
}
