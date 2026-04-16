using WS.WEB.Modules.Support.Core;

namespace WS.WEB.Core
{
    public static class AppInfo
    {
        public static string CompanyName { get; set; } = "DRMA Tech";
        public static string CompanyWebSite { get; set; } = $"https://www.drma-tech.com";

        public static string Title { get; set; } = "WebStandards";
        public static string Subtitle { get; set; } = "Web Developer Tools";
        public static string Domain { get; set; } = "web-standards";
        public static string WebSite { get; set; } = $"https://www.{Domain}.com";
        public static int Year { get; set; } = 2021;

        public static readonly string? _windowsId = "9pcf521f26lz";
        public static readonly string? _googleId = null;
        public static readonly string? _appleId = null;
        public static readonly string? _huaweiId = null;
        public static readonly string? _xiaomiId = null;
        public static readonly string? _amazonId = null;

        public static readonly StoreLink[] Stores =
        [
            new(Platform.windows, "Microsoft Store", $"https://apps.microsoft.com/detail/{_windowsId}", "/logo/microsoft-store.png" ),
            //new(Platform.play, "Google Play", $"https://play.google.com/store/apps/details?id={_googleId}", "/logo/google-play.png" ),
            //new(Platform.ios, "App Store", $"https://apps.apple.com/us/app/{_appleId}", "/logo/app-store.png" ),
            //new(Platform.huawei, "Huawei AppGallery", $"https://appgallery.huawei.com/app/{_huaweiId}", "/logo/huawei.png" ),
            //new(Platform.xiaomi, "Xiaomi GetApps", $"https://global.app.mi.com/details?id={_xiaomiId}", "/logo/xiaomi.png" ),
            //new(Platform.amazon, "Amazon Appstore", $"https://www.amazon.com/gp/product/{_amazonId}", "/logo/amazon.png" )
        ];

        public static readonly ProductLink[] Products =
        [
            new("Streaming Discovery", "Manage, Track, Discover", "https://www.streamingdiscovery.com", "/logo/streamingdiscovery.png", true ),
            new("Modern Matchmaker", "Find your life partner", "https://www.modern-matchmaker.com", "/logo/modern-matchmaker.png", true ),
            new("My Next Spot", "Match your next destination", "https://www.my-next-spot.com", "/logo/next-spot.png", false ),
            //new("WebStandards", "Web Developer Tools", "https://www.web-standards.com/", "/logo/webstandards.png", false ),
       ];
    }
}