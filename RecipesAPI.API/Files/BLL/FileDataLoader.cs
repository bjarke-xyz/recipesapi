using RecipesAPI.API.Files.DAL;

namespace RecipesAPI.API.Files.BLL;

public class FileDataLoader : BatchDataLoader<string, FileDto>
{
    private readonly IFileService fileService;

    public FileDataLoader(IFileService fileService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.fileService = fileService;
    }

    protected override async Task<IReadOnlyDictionary<string, FileDto>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var files = await fileService.GetFiles(keys, cancellationToken);
        return files;
    }
}