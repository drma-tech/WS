using WS.WEB.Modules.Help.Core;

namespace WS.WEB.Core
{
    public static class AppInfo
    {
        public static string CompanyName { get; set; } = "DRMA Tech";
        public static string CompanyWebSite { get; set; } = $"https://www.drma-tech.com";

        public static string Title { get; set; } = "Web Standards";
        public static string Domain { get; set; } = "web-standards";
        public static string WebSite { get; set; } = $"https://{Domain}.com";
        public static int Year { get; set; } = 2021;

        public static readonly string? WindowsId = "9pcf521f26lz";
        public static readonly string? GoogleId;
        public static readonly string? AppleId;
        public static readonly string? HuaweiId;
        public static readonly string? XiaomiId;
        public static readonly string? AmazonId;

        public static readonly StoreLink[] Stores =
        [
            new(Platform.windows, "Microsoft Store", $"https://apps.microsoft.com/detail/{WindowsId}", "/logo/microsoft-store.png" ),
            new(Platform.play, "Google Play", $"https://play.google.com/store/apps/details?id={GoogleId}", "/logo/google-play.png" ),
            new(Platform.ios, "App Store", $"https://apps.apple.com/us/app/{AppleId}", "/logo/app-store.png" ),
            new(Platform.huawei, "Huawei AppGallery", $"https://appgallery.huawei.com/app/{HuaweiId}", "/logo/huawei.png" ),
            new(Platform.xiaomi, "Xiaomi GetApps", $"https://global.app.mi.com/details?id={XiaomiId}", "/logo/xiaomi.png" ),
            new(Platform.amazon, "Amazon Appstore", $"https://www.amazon.com/gp/product/{AmazonId}", "/logo/amazon.png" ),
        ];

        public static readonly ProductLink[] Products =
        [
            new("Streaming Discovery", "Discover Movies and Series on Streaming Platforms", "https://streamingdiscovery.com", "/logo/streamingdiscovery.png", live: true ),
            new("Modern Matchmaker", "Find a compatible partner through Smart Matchmaking", "https://modern-matchmaker.com", "/logo/modern-matchmaker.png", live: true ),
            new("My Next Spot", "Find the Best Cities and Countries to Live or Travel", "https://my-next-spot.com", "/logo/next-spot.png", live: true ),
            //new("Web Standards", "Web Standards Generator for Websites and PWAs", "https://web-standards.com", "/logo/webstandards.png", live: false ),
            //new("Shared Home", "Room rentals, shared homes and community", "https://shared-home.com", "/logo/shared-home.png", true ),
       ];
    }
}
