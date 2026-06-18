using System.Text.Json.Serialization;
using WS.Shared.Models;

namespace WS.WEB.Core.Api
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(bool?))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Platform?))]
    [JsonSerializable(typeof(AppLanguage?))]
    [JsonSerializable(typeof(HashSet<DateTime>))]
    [JsonSerializable(typeof(IndexNowModel))]
    internal partial class JavascriptContext : JsonSerializerContext
    {
    }
}
