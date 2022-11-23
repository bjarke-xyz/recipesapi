using RecipesAPI.API.Admin.Common;
using RecipesAPI.API.Files.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Files.BLL;

public interface IFileService : ICacheKeyGetter
{
    Task<UploadUrlTicket> GetSignedUploadUrl(string bucket, string key, string fileId, string contentType, ulong contentLength, string fileName, CancellationToken cancellationToken);
    Task<UploadUrlTicket?> GetUploadUrlTicket(string code);
    Task<FileDto?> GetFile(string id, CancellationToken cancellationToken);
    Task<Dictionary<string, FileDto>> GetFiles(IReadOnlyList<string> ids, CancellationToken cancellationToken);
    Task<Stream?> GetFileContent(string fileId, CancellationToken cancellationToken);
    Task<Stream?> GetFileContent(string bucket, string key, CancellationToken cancellationToken);
    string GetPublicUrl(FileDto file);
    Task<FileDto?> SaveFile(FileDto file, CancellationToken cancellationToken);
    Task<FileDto?> SaveFile(FileDto file, Stream content, CancellationToken cancellationToken);
    Task DeleteFile(FileDto file, CancellationToken cancellationToken);
}

public class FileService : IFileService
{
    private readonly FileRepository fileRepository;
    private readonly ICacheProvider cache;
    private readonly IStorageClient storageClient;

    private readonly ILogger<FileService> logger;

    private string FileCacheKey(string id) => $"GetFile:{id}";
    private string UploadTicketKey(string code) => $"UploadTicket:{code}";

    public FileService(FileRepository fileRepository, ICacheProvider cache, IStorageClient storageClient, ILogger<FileService> logger)
    {
        this.fileRepository = fileRepository;
        this.cache = cache;
        this.storageClient = storageClient;
        this.logger = logger;
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>{
                FileCacheKey("")
            },
            ResourceType = CachedResourceTypeHelper.FILES,
        };
    }

    public async Task<UploadUrlTicket> GetSignedUploadUrl(string bucket, string key, string fileId, string contentType, ulong contentLength, string fileName, CancellationToken cancellationToken)
    {
        var uploadUrl = await storageClient.GetSignedUploadUrl(bucket, key, contentType, contentLength, cancellationToken);
        var ticket = new UploadUrlTicket
        {
            Bucket = bucket,
            Key = key,
            Code = Guid.NewGuid().ToString(),
            Url = uploadUrl,
            FileId = fileId,
            ContentType = contentType,
            FileName = fileName,
        };
        await cache.Put(UploadTicketKey(ticket.Code), ticket, expiration: TimeSpan.FromHours(24));
        return ticket;
    }

    public async Task<UploadUrlTicket?> GetUploadUrlTicket(string code)
    {
        return await cache.Get<UploadUrlTicket>(UploadTicketKey(code));
    }

    public async Task<FileDto?> GetFile(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<FileDto>(FileCacheKey(id));
        if (cached == null)
        {
            cached = await fileRepository.GetFile(id, cancellationToken);
            if (cached != null)
            {
                await cache.Put(FileCacheKey(id), cached);
            }
        }
        return cached;
    }

    public async Task<Dictionary<string, FileDto>> GetFiles(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        var mutableIds = ids.ToList();
        var result = new Dictionary<string, FileDto>();
        var fromCache = await cache.Get<FileDto>(ids.Select(x => FileCacheKey(x)).ToList(), cancellationToken);
        foreach (var file in fromCache)
        {
            if (file != null)
            {
                result[file.Id] = file;
                mutableIds.Remove(file.Id);
            }
        }

        var fromDb = await fileRepository.GetFiles(mutableIds, cancellationToken);
        foreach (var file in fromDb)
        {
            await cache.Put(FileCacheKey(file.Value.Id), file.Value);
            result[file.Key] = file.Value;
        }
        return result;
    }

    public async Task<Stream?> GetFileContent(string fileId, CancellationToken cancellationToken)
    {
        var file = await GetFile(fileId, cancellationToken);
        if (file == null) return null;

        var stream = await storageClient.GetStream(file.Bucket, file.Key, cancellationToken);
        return stream;
    }

    public async Task<Stream?> GetFileContent(string bucket, string key, CancellationToken cancellationToken)
    {
        return await storageClient.GetStream(bucket, key, cancellationToken);
    }

    public string GetPublicUrl(FileDto file)
    {
        // return $"https://pub-fc8159a8900d44e2b3f022917f202fc1.r2.dev/{file.Key}";
        return $"https://storage.googleapis.com/{file.Bucket}/{file.Key}";
    }

    public async Task<FileDto?> SaveFile(FileDto file, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Id))
        {
            file.Id = Guid.NewGuid().ToString();
        }
        await fileRepository.SaveFile(file, cancellationToken);
        await cache.Remove(FileCacheKey(file.Id));
        return await GetFile(file.Id, cancellationToken);
    }

    public async Task<FileDto?> SaveFile(FileDto file, Stream content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Id))
        {
            file.Id = Guid.NewGuid().ToString();
        }
        await fileRepository.SaveFile(file, cancellationToken);
        await cache.Remove(FileCacheKey(file.Id));
        var createdFile = await GetFile(file.Id, cancellationToken);
        if (createdFile != null)
        {
            await storageClient.PutStream(file.Bucket, file.Key, content, createdFile.ContentType, cancellationToken);
        }
        return createdFile;
    }

    public async Task DeleteFile(FileDto file, CancellationToken cancellationToken)
    {
        try
        {
            await fileRepository.DeleteFile(file, cancellationToken);
            await cache.Remove(FileCacheKey(file.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to delete file with id {id}", file.Id);
            throw;
        }

        // await storageClient.Delete(file.Bucket, file.Key, cancellationToken);
    }

}

public class UploadUrlTicket
{
    public string Url { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string FileId { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string FileName { get; set; } = default!;
}