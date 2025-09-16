using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Sas;

using Medallion.Threading.Azure;

namespace SE.TridentContrib.Extensions.Azure.Blobs;

public abstract class BlobStorage : IBlobStorage
{
    private readonly string _connectionString;

    protected BlobStorage(string connectionString, string defaultContainerName)
    {
        _connectionString = connectionString;
        DefaultContainerName = defaultContainerName;
    }

    protected abstract string Prefix { get; }

    public string DefaultContainerName { get; }

    public async Task<bool> Exists(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        return await client.ExistsAsync(cancellationToken);
    }

    public async Task Upload(string containerName, string blobName, Stream stream, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        await client.UploadAsync(stream, true, cancellationToken);
    }

    public async Task<Stream> Download(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        var response = await client.DownloadAsync(cancellationToken);
        return response.Value.Content;
    }

    public async Task<bool> Delete(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        var response = await client.DeleteIfExistsAsync(default, default, cancellationToken);
        return response.Value;
    }

    public Uri GetUploadUri(string containerName, string blobName)
    {
        var client = GetBlobClient(containerName, blobName);
        var blobSasBuilder = new BlobSasBuilder(BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(5))
        {
            BlobName = GetFullBlobName(blobName),
        };

        return client.GenerateSasUri(blobSasBuilder);
    }

    public Uri GetDownloadUri(string containerName, string blobName, string contentDisposition, string contentType)
    {
        var client = GetBlobClient(containerName, blobName);
        var blobSasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(5))
        {
            BlobName = GetFullBlobName(blobName),
            ContentDisposition = contentDisposition,
            ContentType = contentType,
        };

        return client.GenerateSasUri(blobSasBuilder);
    }

    public async Task SetMetadata(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        await client.SetMetadataAsync(metadata, default, cancellationToken);
    }

    public async Task SetTags(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        await client.SetTagsAsync(tags, default, cancellationToken);
    }

    public async Task<IDictionary<string, string>> GetMetadata(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var client = GetBlobClient(containerName, blobName);
        var response = await client.GetPropertiesAsync(default, cancellationToken);
        var metadata = response.Value.Metadata;
        metadata["Content-Type"] = response.Value.ContentType;
        return metadata;
    }

    public async Task<T> AcquireLeaseAndExecute<T>(Func<Task<T>> task,
                                                   string leaseBlobName,
                                                   string containerName = "lease-objects",
                                                   TimeSpan? timeout = null,
                                                   CancellationToken cancellationToken = default)
    {
        var distributedLock = new AzureBlobLeaseDistributedLock(new(_connectionString, containerName), leaseBlobName);
        await using var lockHandle = await distributedLock.AcquireAsync(timeout ?? TimeSpan.FromSeconds(90), cancellationToken);
        return await task();
    }

    public async Task<(bool success, T value)> TryAcquireLeaseAndExecute<T>(Func<Task<T>> task,
                                                                            string leaseBlobName,
                                                                            string containerName = "lease-objects",
                                                                            TimeSpan timeout = default,
                                                                            CancellationToken cancellationToken = default)
    {
        var distributedLock = new AzureBlobLeaseDistributedLock(new(_connectionString, containerName), leaseBlobName);
        await using var lockHandle = await distributedLock.TryAcquireAsync(timeout, cancellationToken);
        return lockHandle == null ? (false, default) : (true, await task());
    }

    protected virtual string GetContainerName(string containerName)
    {
        return string.IsNullOrWhiteSpace(containerName) ? DefaultContainerName : containerName;
    }

    protected virtual string GetFullBlobName(string blobName)
    {
        return Prefix == null ? blobName : $"{Prefix}/{blobName}";
    }

    protected virtual BlobClient GetBlobClient(string containerName, string blobName)
    {
        containerName = GetContainerName(containerName);
        blobName = GetFullBlobName(blobName);
        var blobContainerClient = new BlobContainerClient(_connectionString, containerName);
        return blobContainerClient.GetBlobClient(blobName);
    }
}
