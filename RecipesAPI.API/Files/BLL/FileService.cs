using RecipesAPI.Admin.Common;
using RecipesAPI.Files.DAL;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Files.BLL;

public interface IFileService : ICacheKeyGetter
{
    Task<FileDto?> GetFile(string id, CancellationToken cancellationToken);
    Task<Dictionary<string, FileDto>> GetFiles(IReadOnlyList<string> ids, CancellationToken cancellationToken);
    Task<Stream?> GetFileContent(string fileId, CancellationToken cancellationToken);
    string GetPublicUrl(FileDto file);
    Task<FileDto?> SaveFile(FileDto file, CancellationToken cancellationToken);
    Task<FileDto?> SaveFile(FileDto file, Stream content, CancellationToken cancellationToken);
}

public class FileService : IFileService
{
    private readonly FileRepository fileRepository;
    private readonly ICacheProvider cache;
    private readonly IStorageClient storageClient;

    private string FileCacheKey(string id) => $"GetFile:{id}";

    public FileService(FileRepository fileRepository, ICacheProvider cache, IStorageClient storageClient)
    {
        this.fileRepository = fileRepository;
        this.cache = cache;
        this.storageClient = storageClient;
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

}