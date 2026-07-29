namespace WS.Shared.Core;

using Json = System.Text.Json.Serialization;
using Nsoft = Newtonsoft.Json;

public static class CosmosDocumentHelper
{
    public static string? RemovePrefix(this string? id)
    {
        if (id.Empty()) throw new UnhandledException("id is required");

        var index = id.LastIndexOf(':');

        return index >= 0 ? id[(index + 1)..] : id;
    }
}

public interface ICosmosIdentity
{
    string Id { get; }
    string? RawId { get; }
    object Key { get; }
}

/// <summary>
/// Every class inheriting from this base class must have an ID parameter that is strictly named `id`.
/// </summary>
/// <param name="id"></param>
public abstract class CosmosDocument(ICosmosIdentity identity)
{
    [Json.JsonPropertyName("id")]
    [Nsoft.JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = identity.Id;

    [Json.JsonPropertyName("_tsCreated")]
    [Nsoft.JsonProperty(PropertyName = "_tsCreated")]
    public long? TimestampCreated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Json.JsonPropertyName("_ts")]
    [Nsoft.JsonProperty(PropertyName = "_ts")]
    public long TimestampUpdated { get; set; }

    [Json.JsonIgnore]
    [Nsoft.JsonIgnore]
    public DateTime? DateTimeCreated => TimestampCreated.HasValue ? DateTimeOffset.FromUnixTimeSeconds(TimestampCreated.Value).UtcDateTime : null;

    [Json.JsonIgnore]
    [Nsoft.JsonIgnore]
    public DateTime DateTimeUpdated => DateTimeOffset.FromUnixTimeSeconds(TimestampUpdated).UtcDateTime;

    [Json.JsonIgnore]
    [Nsoft.JsonIgnore]
    public ICosmosIdentity Identity { get; } = identity;
}
