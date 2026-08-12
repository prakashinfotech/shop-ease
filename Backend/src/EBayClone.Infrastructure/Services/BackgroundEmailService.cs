using EBayClone.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EBayClone.Infrastructure.Services;

public class BackgroundEmailService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundEmailService> logger) : IBackgroundEmailService
{
    public void EnqueueEmail(Func<IEmailService, Task> action)
    {
        // Fire and forget - the task runs in its own scope
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                logger.LogDebug("[BG-EMAIL] Starting background email task...");
                await action(emailService);
                logger.LogDebug("[BG-EMAIL] Background email task completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[BG-EMAIL] Critical failure in background email execution.");
            }
        });
    }
}
