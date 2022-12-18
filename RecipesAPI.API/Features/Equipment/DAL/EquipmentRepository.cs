using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Equipment.Common;

namespace RecipesAPI.API.Features.Equipment.DAL;

public class EquipmentRepository
{
    private readonly ILogger<EquipmentRepository> logger;
    private readonly FirestoreDb db;

    private const string equipmentCollection = "equipment";

    public EquipmentRepository(FirestoreDb db, ILogger<EquipmentRepository> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<List<EquipmentItem>> GetEquipment(CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(equipmentCollection).GetSnapshotAsync(cancellationToken);
        var equipmentList = new List<EquipmentItem>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dto = doc.ConvertTo<EquipmentItemDto>();
                var equipment = EquipmentMapper.MapDto(dto);
                equipmentList.Add(equipment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to donvert equipment dto");
            }
        }
        return equipmentList;
    }
    public async Task<Dictionary<string, EquipmentItem>> GetEquipment(List<string> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0) return new Dictionary<string, EquipmentItem>();
        var snapshot = await db.Collection(equipmentCollection).WhereIn(FieldPath.DocumentId, ids).GetSnapshotAsync(cancellationToken);
        var result = new Dictionary<string, EquipmentItem>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dto = doc.ConvertTo<EquipmentItemDto>();
                var equipmentItem = EquipmentMapper.MapDto(dto);
                result[equipmentItem.Id] = equipmentItem;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to donvert equipment dto");
            }
        }
        return result;
    }

    public async Task<EquipmentItem?> GetEquipment(string id, CancellationToken cancellationToken)
    {
        var doc = await db.Collection(equipmentCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
        {
            return null;
        }
        try
        {
            var dto = doc.ConvertTo<EquipmentItemDto>();
            var equipment = EquipmentMapper.MapDto(dto);
            return equipment;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to donvert equipment dto");
            return null;
        }
    }

    public async Task SaveEquipment(EquipmentItem equipment, CancellationToken cancellationToken)
    {
        var dto = EquipmentMapper.Map(equipment);
        await db.Collection(equipmentCollection).Document(dto.Id).SetAsync(dto, null, cancellationToken);
    }

    public async Task DeleteEquipment(EquipmentItem equipment, CancellationToken cancellationToken)
    {
        var dto = EquipmentMapper.Map(equipment);
        await db.Collection(equipmentCollection).Document(dto.Id).DeleteAsync(null, cancellationToken);
    }
}