namespace RecipesAPI.API.Infrastructure.Serializers;

public interface ISerializer
{
    string GetTag();
    Task<T?> Deserialize<T>(byte[] bytes, CancellationToken ct) where T : class;
    Task<byte[]> Serialize<T>(T obj, CancellationToken ct);
}

public class MyJsonSerializer : ISerializer
{
    public const string Tag = "js";
    public async Task<T?> Deserialize<T>(byte[] bytes, CancellationToken ct) where T : class
    {
        using var stream = new MemoryStream(bytes);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
    }

    public string GetTag()
    {
        return Tag;
    }

    public async Task<byte[]> Serialize<T>(T obj, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await System.Text.Json.JsonSerializer.SerializeAsync(ms, obj, cancellationToken: ct);
        return ms.ToArray();
    }
}


public class MyMessagePackSerializer : ISerializer
{
    public const string Tag = "mp";
    private static bool EmptyCollectionHack(byte[] val)
    {
        return val.Length == 1 && (val[0] == 144 || val[0] == 128);
    }

    public async Task<T?> Deserialize<T>(byte[] bytes, CancellationToken ct) where T : class
    {
        if (EmptyCollectionHack(bytes))
        {
            // hack to fix empty collections throwing an error on deserialize
            var empty = (T?)Activator.CreateInstance(typeof(T));
            return empty;
        }
        using var stream = new MemoryStream(bytes);
        return await MessagePack.MessagePackSerializer.DeserializeAsync<T>(stream, MessagePack.Resolvers.ContractlessStandardResolver.Options, ct);
    }

    public string GetTag()
    {
        return Tag;
    }

    public async Task<byte[]> Serialize<T>(T obj, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await MessagePack.MessagePackSerializer.SerializeAsync<T>(stream, obj, MessagePack.Resolvers.ContractlessStandardResolver.Options, ct);
        return stream.ToArray();
    }
}


// NOTE: does not work, crashes program with stack overflow on first use
public class MyCborSerializer : ISerializer
{
    public const string Tag = "cb";

    public async Task<T?> Deserialize<T>(byte[] bytes, CancellationToken ct) where T : class
    {
        using var stream = new MemoryStream(bytes);
        return await Dahomey.Cbor.Cbor.DeserializeAsync<T>(stream, token: ct);
    }

    public string GetTag()
    {
        return Tag;
    }

    public async Task<byte[]> Serialize<T>(T obj, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await Dahomey.Cbor.Cbor.SerializeAsync<T>(obj, stream);
        return stream.ToArray();
    }
}
