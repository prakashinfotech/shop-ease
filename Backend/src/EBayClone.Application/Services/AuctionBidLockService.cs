using System.Collections.Concurrent;

namespace EBayClone.Application.Services;

public class AuctionBidLockService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public SemaphoreSlim GetLock(Guid listingId) =>
        _locks.GetOrAdd(listingId, _ => new SemaphoreSlim(1, 1));
}
