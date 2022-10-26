using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Google.Cloud.Storage.V1;

namespace RecipesAPI.Infrastructure;

public class S3StorageClient : IStorageClient
{
    private readonly IAmazonS3 client;

    public S3StorageClient(string accessKey, string secretKey, string accountId)
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        });
    }

    public async Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            Key = key,
            InputStream = data,
            BucketName = bucket,
            ContentType = contentType,
            DisablePayloadSigning = true,
        };
        var response = await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<byte[]?> Get(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return null;
        using var stream = response.ResponseStream;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes, 0, (int)stream.Length, cancellationToken);
        return bytes;
    }

    public async Task<Stream?> GetStream(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return null;
        return response.ResponseStream;
    }

}

public class GoogleStorageClient : IStorageClient
{
    private readonly Google.Cloud.Storage.V1.StorageClient client;
    private readonly ILogger<GoogleStorageClient> logger;

    public GoogleStorageClient(StorageClient storageClient, ILogger<GoogleStorageClient> logger)
    {
        this.client = storageClient;
        this.logger = logger;
    }

    public async Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken)
    {
        await client.UploadObjectAsync(bucket, key, contentType, data, null, cancellationToken);
    }

    public async Task<byte[]?> Get(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var ms = new MemoryStream();
            var resp = await client.DownloadObjectAsync(bucket, key, ms, null, cancellationToken);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to download from {bucket} {key}", bucket, key);
            return null;
        }
    }

    public async Task<Stream?> GetStream(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var ms = new MemoryStream();
            var resp = await client.DownloadObjectAsync(bucket, key, ms, null, cancellationToken);
            return ms;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to download from {bucket} {key}", bucket, key);
            return null;
        }
    }
}

public interface IStorageClient
{
    Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken);
    Task<byte[]?> Get(string bucket, string key, CancellationToken cancellationToken);
    Task<Stream?> GetStream(string bucket, string key, CancellationToken cancellationToken);
}