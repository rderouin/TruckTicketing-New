using System;
using System.Threading;
using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.TridentContrib.Extensions.Threading;

public interface IDistributedRateLimiter
{
    Task<T> Execute<T>(Func<Task<T>> task,
                       IBlobStorage blobStorage,
                       string leaseBlobName,
                       int slots,
                       TimeSpan? timeout,
                       CancellationToken cancellationToken = default,
                       string containerName = "lease-objects");
}
