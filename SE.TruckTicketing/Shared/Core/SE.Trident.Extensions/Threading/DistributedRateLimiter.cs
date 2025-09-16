using System;
using System.Threading;
using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.TridentContrib.Extensions.Threading;

public class DistributedRateLimiter : IDistributedRateLimiter
{
    public async Task<T> Execute<T>(Func<Task<T>> task,
                                    IBlobStorage blobStorage,
                                    string leaseBlobName,
                                    int slots,
                                    TimeSpan? timeout,
                                    CancellationToken cancellationToken = default,
                                    string containerName = "lease-objects")
    {
        // try finding a free slot from the pre-allocated number of slots
        for (var i = 0; i < slots; i++)
        {
            // attempt to lease a specific lock (i) with 0 timeout so that if it's busy, try acquiring another slot
            // the success flag indicates if the lease was acquired and the task is executed, thus resulting in a value
            var (success, value) = await blobStorage.TryAcquireLeaseAndExecute(task, GetFullLeaseBlobName(i), containerName, default, cancellationToken);

            // there was a free slot and the task is executed, the result is available
            if (success)
            {
                return value;
            }
        }

        // all slots are busy, wait for a random slot, but this time use the provided timeout
        // if the wait time is up, the exception will be thrown
        var slot = Random.Shared.Next(slots);
        var awaitedValue = await blobStorage.AcquireLeaseAndExecute(task, GetFullLeaseBlobName(slot), containerName, timeout, cancellationToken);
        return awaitedValue;

        string GetFullLeaseBlobName(int lockNumber)
        {
            return $"{leaseBlobName}-{lockNumber}";
        }
    }
}
