namespace WS.WEB.Layout
{
    public partial class FooterComponent : IDisposable
    {
        private string Culture => Navigation.GetCulture();

        private bool _blockedActions => Navigation.Uri.Contains("register-user", StringComparison.OrdinalIgnoreCase) || Navigation.Uri.Contains("ask-consent", StringComparison.OrdinalIgnoreCase);
        private Platform? CurrentPlatform;
        protected CancellationTokenSource Cts { get; } = new();

        protected override void OnInitialized()
        {
            AppStateStatic.BreakpointChanged.Subscribe(breakpoint => StateHasChanged(), Cts.Token);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CurrentPlatform = await AppStateStatic.GetPlatform(JsRuntime, Cts.Token);
            }
        }

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                Cts.Cancel();
                Cts.Dispose();
            }

            isDisposed = true;
        }
    }
}