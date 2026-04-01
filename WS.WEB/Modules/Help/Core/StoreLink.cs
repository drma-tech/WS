namespace WS.WEB.Modules.Support.Core
{
    public class StoreLink(WS.Shared.Enums.Platform platform, string store, string url, string logo)
    {
        public WS.Shared.Enums.Platform Platform { get; set; } = platform;
        public string Store { get; set; } = store;
        public string Url { get; set; } = url;
        public string Logo { get; set; } = logo;
    }
}