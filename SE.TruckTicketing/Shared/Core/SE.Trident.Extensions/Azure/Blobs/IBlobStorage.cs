using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SE.TridentContrib.Extensions.Azure.Blobs;

public interface IBlobStorage
{
    public string DefaultContainerName { get; }

    Task<bool> Exists(string containerName, string blobName, CancellationToken cancellationToken = default);

    Task Upload(string containerName, string blobName, Stream stream, CancellationToken cancellationToken = default);

    Task<Stream> Download(string containerName, string blobName, CancellationToken cancellationToken = default);

    Task<bool> Delete(string containerName, string blobName, CancellationToken cancellationToken = default);

    Uri GetUploadUri(string containerName, string blobName);

    Uri GetDownloadUri(string containerName, string blobName, string contentDisposition, string contentType);

    Task SetMetadata(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

    Task SetTags(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken = default);

    Task<IDictionary<string, string>> GetMetadata(string containerName, string blobName, CancellationToken cancellationToken = default);

    Task<T> AcquireLeaseAndExecute<T>(Func<Task<T>> task,
                                      string leaseBlobName,
                                      string containerName = "lease-objects",
                                      TimeSpan? timeout = null,
                                      CancellationToken cancellationToken = default);

    Task<(bool success, T value)> TryAcquireLeaseAndExecute<T>(Func<Task<T>> task,
                                                               string leaseBlobName,
                                                               string containerName = "lease-objects",
                                                               TimeSpan timeout = default,
                                                               CancellationToken cancellationToken = default);
}
