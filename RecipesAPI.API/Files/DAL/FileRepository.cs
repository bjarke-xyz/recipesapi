using Google.Cloud.Firestore;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Files.DAL;

public class FileRepository
{
    private readonly FirestoreDb db;

    private const string filesCollection = "files";

    public FileRepository(FirestoreDb db)
    {
        this.db = db;
    }

    public async Task<FileDto?> GetFile(string id, CancellationToken cancellationToken)
    {
        var doc = await db.Collection(filesCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
        {
            return null;
        }
        var dto = doc.ConvertTo<FileDto>();
        if (!dto.CreatedAt.HasValue)
        {
            dto.CreatedAt = doc.CreateTime?.ToDateTimeOffset();
        }
        return dto;
    }

    public async Task SaveFile(FileDto file, CancellationToken cancellationToken)
    {
        await db.Collection(filesCollection).Document(file.Id).SetAsync(file, null, cancellationToken);
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

    public DateTimeOffset? CreatedAt { get; set; }
}