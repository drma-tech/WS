using System.Text.Json.Serialization;
using WS.WEB.Modules.Search.Models;

namespace WS.WEB.Core.Api
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(WebAppManifest))]
    internal sealed partial class ApiContext : JsonSerializerContext
    {
    }
}