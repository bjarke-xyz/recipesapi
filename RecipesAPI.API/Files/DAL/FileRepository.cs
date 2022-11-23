using Google.Cloud.Firestore;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Files.DAL;

public class FileRepository
{
    private readonly FirestoreDb db;
    private readonly ILogger<FileRepository> logger;

    private const string filesCollection = "files";

    public FileRepository(FirestoreDb db, ILogger<FileRepository> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<FileDto?> GetFile(string id, CancellationToken cancellationToken)
    {
        var doc = await db.Collection(filesCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
        {
            return null;
        }
        var dto = doc.ConvertTo<FileDto>();
        if (dto.DeletedAt.HasValue)
        {
            return null;
        }
        if (!dto.CreatedAt.HasValue)
        {
            dto.CreatedAt = doc.CreateTime?.ToDateTimeOffset();
        }
        if (string.IsNullOrEmpty(dto.Id))
        {
            dto.Id = doc.Id;
        }
        dto.Id = dto.Id.Trim();
        return dto;
    }

    public async Task<Dictionary<string, FileDto>> GetFiles(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0)
        {
            return new Dictionary<string, FileDto>();
        }
        var snapshot = await db.Collection(filesCollection).WhereIn(FieldPath.DocumentId, ids).GetSnapshotAsync(cancellationToken);
        var result = new Dictionary<string, FileDto>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dto = doc.ConvertTo<FileDto>();
                if (dto.DeletedAt.HasValue)
                {
                    continue;
                }
                if (!dto.CreatedAt.HasValue)
                {
                    dto.CreatedAt = doc.CreateTime?.ToDateTimeOffset();
                }
                if (string.IsNullOrEmpty(dto.Id))
                {
                    dto.Id = doc.Id;
                }
                dto.Id = dto.Id.Trim();
                result[dto.Id] = dto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to convert file dto with id {id}", doc.Id);
            }
        }
        return result;
    }

    public async Task SaveFile(FileDto file, CancellationToken cancellationToken)
    {
        await db.Collection(filesCollection).Document(file.Id).SetAsync(file, null, cancellationToken);
    }

    public async Task DeleteFile(FileDto file, CancellationToken cancellationToken)
    {
        await db.Collection(filesCollection).Document(file.Id).UpdateAsync("deletedAt", DateTime.UtcNow, null, cancellationToken);
    }
}

[FirestoreData]
public class FileDto
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = default!;
    [FirestoreProperty("bucket")]
    public string Bucket { get; set; } = default!;
    [FirestoreProperty("key")]
    public string Key { get; set; } = default!;
    [FirestoreProperty("contentType")]
    public string ContentType { get; set; } = default!;
    [FirestoreProperty("size")]
    public long Size { get; set; } = default!;
    [FirestoreProperty("fileName")]
    public string FileName { get; set; } = default!;
    [FirestoreProperty("blurHash")]
    public string? BlurHash { get; set; }
    [FirestoreProperty("dimensions")]
    public ImageDimensionsDto? Dimensions { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}


[FirestoreData]
public class ImageDimensionsDto
{
    [FirestoreProperty("original")]
    public ImageDimensionDto? Original { get; set; }
    [FirestoreProperty("blurHash")]
    public ImageDimensionDto? BlurHash { get; set; }
}

[FirestoreData]
public class ImageDimensionDto
{
    [FirestoreProperty("width")]
    public int Width { get; set; }
    [FirestoreProperty("height")]
    public int Height { get; set; }
}