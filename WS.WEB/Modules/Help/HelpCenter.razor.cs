namespace WS.WEB.Modules.Help
{
    public partial class HelpCenter
    {
        private WS.Shared.Enums.Platform? CurrentPlatform;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CurrentPlatform = await AppStateStatic.GetPlatform(JsRuntime, Cts.Token);
            }
        }

        private async Task FeedbackClick()
        {
            await JsRuntime.Window().InvokeVoidAsync("eval", "Userback && Userback.openForm('general', 'form');");
        }

        private async Task ShowCacheClick()
        {
            await JsRuntime.Utils().ShowCache(Cts.Token);
        }

        private async Task ClearCacheClick()
        {
            await JsRuntime.Utils().ClearAllStorage();
        }
    }
}