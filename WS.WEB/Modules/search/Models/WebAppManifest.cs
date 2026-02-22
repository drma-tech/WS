using System.Text.Json.Serialization;

namespace WS.WEB.Modules.Search.Models
{
    public class WebAppManifest
    {
        public string? Id { get; set; } = "myapp";
        public string? Name { get; set; } = "My App";

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; } = "App";

        public string? Description { get; set; } = "A sample web application manifest.";

        [JsonPropertyName("background_color")]
        public string? BackgroundColor { get; set; } = "#ffffff";

        [JsonPropertyName("theme_color")]
        public string? ThemeColor { get; set; } = "#0d6efd";

        [JsonPropertyName("start_url")]
        public string? StartUrl { get; set; } = "/";

        public string? Dir { get; set; }
        public string? Scope { get; set; } = "/";
        public string? Lang { get; set; } = "en-US";
        public string? Orientation { get; set; }
        public string? Display { get; set; } = "standalone";

        [JsonPropertyName("display_override")]
        public List<string> DisplayOverride { get; set; } = [];

        [JsonPropertyName("iarc_rating_id")]
        public string? IarcRatingId { get; set; }

        [JsonPropertyName("prefer_related_applications")]
        public bool PreferRelatedApplications { get; set; }

        [JsonPropertyName("related_applications")]
        public List<RelatedApplication> RelatedApplications { get; set; } = [];

        public List<string> Categories { get; set; } = [];
        public List<Icon> Icons { get; set; } = [];
        public List<Screenshot> Screenshots { get; set; } = [];

        [JsonPropertyName("launch_handler")]
        public LaunchHandler LaunchHandler { get; set; } = new();
    }

    public class RelatedApplication
    {
        public required string Platform { get; set; }
        public required string Url { get; set; }
        public required string Id { get; set; }
    }

    public class Icon
    {
        public required string Src { get; set; }
        public string Type { get; set; } = "image/png";
        public string Sizes { get; set; } = "512x512";
    }

    public class Screenshot
    {
        public required string Src { get; set; }
        public required string Type { get; set; }
        public required string Sizes { get; set; }

        [JsonPropertyName("form_factor")]
        public string? FormFactor { get; set; }

        public string? Label { get; set; }
    }

    public class LaunchHandler
    {
        [JsonPropertyName("client_mode")]
        public List<string> ClientMode { get; set; } = [];
    }
}