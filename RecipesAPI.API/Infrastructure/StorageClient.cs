using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace RecipesAPI.Infrastructure;

public class StorageClient : IStorageClient
{
    private readonly IAmazonS3 s3Client;

    public StorageClient(string accessKey, string secretKey, string accountId)
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        s3Client = new AmazonS3Client(credentials, new AmazonS3Config
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
        var response = await s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<byte[]?> Get(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await s3Client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return null;
        using var stream = response.ResponseStream;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes, 0, (int)stream.Length, cancellationToken);
        return bytes;
    }

    public async Task<Stream?> GetStream(string bucket, string key, CancellationToken cancellationToken)
    {
        var response = await s3Client.GetObjectAsync(bucket, key, cancellationToken);
        if (response == null) return null;
        return response.ResponseStream;
    }

}


public interface IStorageClient
{
    Task PutStream(string bucket, string key, Stream data, string contentType, CancellationToken cancellationToken);
    Task<byte[]?> Get(string bucket, string key, CancellationToken cancellationToken);
    Task<Stream?> GetStream(string bucket, string key, CancellationToken cancellationToken);
}