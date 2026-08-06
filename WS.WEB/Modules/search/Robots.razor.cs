using Microsoft.AspNetCore.Components;
using MudBlazor;
using WS.WEB.Modules.Search.Core;

namespace WS.WEB.Modules.Search
{
    public partial class Robots
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public string? Domain { get; set; } = "https://www.mywebsite.com";

        private MudDynamicTabs? DynamicTabs;
        private int UserIndex;

        public RobotsConfig Config { get; set; } = new();

        public string? SitemapRoute { get; set; }

        protected override void OnInitialized()
        {
            var rules = new RobotsRule()
            {
                UserAgent = "*",
                Allow = ["/"],
                Disallow = ["/admin/"],
            };
            Config.Rules.Add(rules);
            Config.Sitemaps.Add($"{Domain}/sitemap.xml");
        }

        public void AddTab(Guid id)
        {
            Config.Rules.Add(new RobotsRule { Id = id, UserAgent = "*" });
            UserIndex = Config.Rules.Count - 1; // Automatically switch to the new tab.
            StateHasChanged();
        }

        public void RemoveTab(Guid id)
        {
            var tabView = Config.Rules.SingleOrDefault((t) => Equals(t.Id, id));
            if (tabView is not null)
            {
                Config.Rules.Remove(tabView);
                StateHasChanged();
            }
        }

        private void AddTabCallback() => AddTab(Guid.NewGuid());

        private void CloseTabCallback(MudTabPanel panel) => RemoveTab((Guid)panel.ID!);
    }
}