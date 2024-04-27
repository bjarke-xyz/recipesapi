using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Equipment.Common;
using RecipesAPI.API.Features.Equipment.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Equipment.BLL;

public class EquipmentService(EquipmentRepository equipmentRepository, ICacheProvider cacheProvider, AffiliateService affiliateService) : ICacheKeyGetter
{
    private readonly EquipmentRepository equipmentRepository = equipmentRepository;
    private readonly AffiliateService affiliateService = affiliateService;

    private readonly ICacheProvider cache = cacheProvider;

    private readonly string GetEquipmentCacheKey = "GetEquipment";
    private static string GetEquipmentByIdCacheKey(string id) => $"GetEquipment:{id}";

    public async Task<List<EquipmentItem>> GetEquipment(CancellationToken cancellationToken)
    {
        var cached = await cache.Get<List<EquipmentItem>>(GetEquipmentCacheKey, cancellationToken);
        if (cached == null)
        {
            cached = await equipmentRepository.GetEquipment(cancellationToken);
            await cache.Put(GetEquipmentCacheKey, cached);
        }
        return cached;
    }
    public async Task<EquipmentItem?> GetEquipment(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<EquipmentItem>(GetEquipmentByIdCacheKey(id), cancellationToken);
        if (cached == null)
        {
            cached = await equipmentRepository.GetEquipment(id, cancellationToken);
            if (cached != null)
            {
                await cache.Put(GetEquipmentByIdCacheKey(id), cached);
            }
        }
        return cached;
    }

    public async Task<Dictionary<string, EquipmentItem>> GetEquipmentByIdsV2(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        var allEquipment = await GetEquipment(cancellationToken);
        var equipmentById = allEquipment.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        var idsToRemove = equipmentById.Keys.Except(ids);
        foreach (var id in idsToRemove)
        {
            equipmentById.Remove(id);
        }
        return equipmentById;
    }

    public async Task<Dictionary<string, EquipmentItem>> GetEquipmentByIds(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        var mutableIds = ids.ToList();
        var result = new Dictionary<string, EquipmentItem>();
        var fromCache = await cache.Get<EquipmentItem>(ids.Select(x => GetEquipmentByIdCacheKey(x)).ToList(), cancellationToken);
        foreach (var (_, item) in fromCache)
        {
            if (item != null)
            {
                result[item.Id] = item;
                mutableIds.Remove(item.Id);
            }
        }
        var fromDb = await equipmentRepository.GetEquipment(mutableIds, cancellationToken);
        foreach (var item in fromDb)
        {
            await cache.Put(GetEquipmentByIdCacheKey(item.Value.Id), item.Value);
            result[item.Key] = item.Value;
            mutableIds.Remove(item.Key);
        }
        return result;
    }

    public async Task<EquipmentItem> SaveEquipment(EquipmentItem equipment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(equipment.Id))
        {
            equipment.Id = Guid.NewGuid().ToString();
        }
        await equipmentRepository.SaveEquipment(equipment, cancellationToken);
        await ClearCache(equipment.Id);
        var savedEquipment = await GetEquipment(equipment.Id, cancellationToken);
        if (savedEquipment == null)
        {
            throw new GraphQLErrorException("Failed to get saved equipment");
        }
        return savedEquipment;
    }

    public async Task DeleteEquipment(EquipmentItem equipment, CancellationToken cancellationToken)
    {
        await equipmentRepository.DeleteEquipment(equipment, cancellationToken);
        await ClearCache(equipment.Id);
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>
            {
                GetEquipmentCacheKey,
                GetEquipmentByIdCacheKey("")
            },
            ResourceType = CachedResourceTypeHelper.EQUIPMENT
        };
    }

    public async Task RunCacheJob(CancellationToken cancellationToken)
    {
        var equipmentList = await GetEquipment(cancellationToken);
        foreach (var equipment in equipmentList)
        {
            _ = await affiliateService.SearchAffiliateItems(equipment.Title, count: 5);
        }
    }

    private async Task ClearCache(string? equipmentId = null)
    {
        await cache.Remove(GetEquipmentCacheKey);
        if (equipmentId != null)
        {
            await cache.Remove(GetEquipmentByIdCacheKey(equipmentId));
        }
    }
}