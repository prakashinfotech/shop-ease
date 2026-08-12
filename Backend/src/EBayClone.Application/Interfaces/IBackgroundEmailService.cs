using EBayClone.Application.Interfaces;

namespace EBayClone.Application.Interfaces;

public interface IBackgroundEmailService
{
    void EnqueueEmail(Func<IEmailService, Task> action);
}
