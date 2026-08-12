using EBayClone.Application.Interfaces;
using EBayClone.Domain.Interfaces;
using EBayClone.Infrastructure.Data;
using EBayClone.Infrastructure.Repositories;
using EBayClone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBayClone.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
            ));

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHostedService<AuctionEndingBackgroundService>();

        return services;
    }
}
