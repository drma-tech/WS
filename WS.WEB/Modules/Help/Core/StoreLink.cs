namespace WS.WEB.Modules.Help.Core
{
    public class StoreLink(Platform platform, string store, string url, string logo)
    {
        public Platform Platform { get; set; } = platform;
        public string Store { get; set; } = store;
        public string Url { get; set; } = url;
        public string Logo { get; set; } = logo;
    }
}