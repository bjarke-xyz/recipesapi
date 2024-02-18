using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace RecipesAPI.API.Infrastructure;

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

    public async Task<(byte[]?, string? contentType)> Get(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return (null, null);
        using var stream = response.ResponseStream;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes, 0, (int)stream.Length, cancellationToken);
        return (bytes, response.Headers.ContentType);
    }

    public async Task<(Stream?, string? contentType)> GetStream(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return (null, null);
        return (response.ResponseStream, response.Headers.ContentType);
    }

    public async Task Delete(string bucket, string key, CancellationToken cancellationToken)
    {
        await client.DeleteObjectAsync(bucket, key, cancellationToken);
    }

    public Task<string> GetSignedUploadUrl(string bucket, string key, string contentType, ulong contentLength, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class GoogleStorageClient : IStorageClient
{
    public const string StorageBucket = "recipes-5000.appspot.com";
    private readonly Google.Cloud.Storage.V1.StorageClient client;
    private readonly ILogger<GoogleStorageClient> logger;

    public GoogleStorageClient(StorageClient storageClient, ILogger<GoogleStorageClient> logger)
    {
        this.client = storageClient;
        this.logger = logger;
    }

    public async Task<string> GetSignedUploadUrl(string bucket, string key, string contentType, ulong contentLength, CancellationToken cancellationToken)
    {
        var credential = (await GoogleCredential.GetApplicationDefaultAsync()).UnderlyingCredential as ServiceAccountCredential;
        var requestTemplate = UrlSigner.RequestTemplate
            .FromBucket(bucket)
            .WithObjectName(key)
            .WithHttpMethod(HttpMethod.Put)
            .WithContentHeaders(new Dictionary<string, IEnumerable<string>>
            {
                { "Content-Type", new []{contentType}},
                { "Content-Length", new[]{contentLength.ToString()}},
            });

        var options = UrlSigner.Options.FromDuration(TimeSpan.FromMinutes(5));
        var urlSigner = UrlSigner.FromCredential(credential);
        var url = urlSigner.Sign(requestTemplate, options);
        return url;
    }

    public async Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken)
    {
        await client.UploadObjectAsync(bucket, key, contentType, data, null, cancellationToken);
    }

    public async Task<(byte[]?, string? contentType)> Get(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var ms = new MemoryStream();
            var resp = await client.DownloadObjectAsync(bucket, key, ms, null, cancellationToken);
            return (ms.ToArray(), resp.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to download from {bucket} {key}", bucket, key);
            return (null, null);
        }
    }

    public async Task<(Stream?, string? contentType)> GetStream(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var ms = new MemoryStream();
            var resp = await client.DownloadObjectAsync(bucket, key, ms, null, cancellationToken);
            return (ms, resp.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to download from {bucket} {key}", bucket, key);
            return (null, null);
        }
    }

    public async Task Delete(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            await client.DeleteObjectAsync(bucket, key, null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to delete object {bucket} {key}", bucket, key);
            throw;
        }
    }
}

public interface IStorageClient
{
    Task Delete(string bucket, string key, CancellationToken cancellationToken);
    Task<string> GetSignedUploadUrl(string bucket, string key, string contentType, ulong contentLength, CancellationToken cancellationToken);
    Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken);
    Task<(byte[]?, string? contentType)> Get(string bucket, string key, CancellationToken cancellationToken);
    Task<(Stream?, string? contentType)> GetStream(string bucket, string key, CancellationToken cancellationToken);
}