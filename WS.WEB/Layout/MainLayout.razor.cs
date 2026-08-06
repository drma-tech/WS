using Microsoft.JSInterop;
using MudBlazor;

namespace WS.WEB.Layout
{
    public partial class MainLayout : IDisposable
    {
        private MudThemeProvider? _mudThemeProvider;
        private bool _darkMode;
        protected CancellationTokenSource Cts { get; } = new();

        protected override void OnInitialized()
        {
            try
            {
                // *************************************
                // attention: avoid using asynchronous calls here, as it may affect static html generation (especially for anonymous users)
                // *************************************

                BufferedEvent.Register(nameof(ShowError), async (string msg) => { await ShowNotificationError(msg); });

                AppStateStatic.DarkModeChanged += dark => { _darkMode = dark; StateHasChanged(); };
                AppStateStatic.BreakpointChanged.Subscribe(breakpoint => StateHasChanged(), Cts.Token);
            }
            catch (Exception ex)
            {
                ex.ProcessException(Snackbar, Logger);
            }
        }

        /// <summary>
        /// Do not process anything here related to authenticated users. (use UserStateChanged instead)
        /// </summary>
        /// <param name="firstRender"></param>
        /// <returns></returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                //Get the value at the beginning (NotifyBrowserViewportChangeAsync is too slow)
                AppStateStatic.Breakpoint = await BrowserViewportService.GetCurrentBreakpointAsync();
                AppStateStatic.Size = AppStateStatic.Breakpoint == Breakpoint.Xs ? Size.Small : Size.Medium;
                AppStateStatic.BreakpointChanged.Publish(AppStateStatic.Breakpoint);

                try
                {
                    await ApplyDarkMode(Cts.Token);
                    await AskUSerForReview(Cts.Token);
                    await RegisterSessionAccesses(Cts.Token);
                    // await ShowOnBoardingPopup(Cts.Token);
                }
                catch (Exception ex)
                {
                    ex.ProcessException(Snackbar, Logger);
                }
            }
        }

        private async Task ApplyDarkMode(CancellationToken cancellationToken)
        {
            var darkMode = await AppStateStatic.GetDarkMode(JsRuntime, Cts.Token);

            if (darkMode == null && _mudThemeProvider != null)
            {
                var system = await _mudThemeProvider.GetSystemDarkModeAsync();
                darkMode = system;

                await JsRuntime.Utils().SetStorage("dark-mode", darkMode ?? false, JavascriptContext.Default.Boolean, cancellationToken);
            }

            AppStateStatic.ChangeDarkMode(darkMode ?? false);
        }

        private async Task AskUSerForReview(CancellationToken cancellationToken)
        {
            var accesses = await JsRuntime.Utils().GetStorage("session-accesses", JavascriptContext.Default.HashSetDateTime, cancellationToken) ?? [];
            var hasPreviousAccess = accesses.Count > 0;
            var lastAccess = hasPreviousAccess ? accesses.Max() : (DateTime?)null;
            bool isTooSoon = false;

            if (lastAccess != null)
            {
                var hoursSinceLast = (DateTime.UtcNow - lastAccess.Value).TotalHours;
                isTooSoon = hoursSinceLast < 24;
            }

            var reviewed = await JsRuntime.Utils().GetStorage("store-reviewed", JavascriptContext.Default.Boolean, cancellationToken);

            accesses.Add(DateTime.UtcNow); //simulate the new access
            bool isOddAccess = accesses.Count % 2 == 1;

            if (accesses.Count >= 3 && isOddAccess && !reviewed && !isTooSoon)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000, cancellationToken); //delay of 5 seconds
                    await DialogService.AskReviewPopup();
                }, cancellationToken);
            }
        }

        private async Task RegisterSessionAccesses(CancellationToken cancellationToken)
        {
            var accesses = await JsRuntime.Utils().GetStorage("session-accesses", JavascriptContext.Default.HashSetDateTime, cancellationToken) ?? [];
            var hasPreviousAccess = accesses.Count > 0;
            var lastAccess = hasPreviousAccess ? accesses.Max() : (DateTime?)null;
            bool isTooSoon = false;

            if (lastAccess != null)
            {
                var hoursSinceLast = (DateTime.UtcNow - lastAccess.Value).TotalHours;
                isTooSoon = hoursSinceLast < 2;
            }

            if (!isTooSoon)
            {
                accesses.Add(DateTime.UtcNow);

                if (accesses.Count > 10) //keep only the last 10 records
                {
                    accesses = [.. accesses.OrderDescending().Take(10)];
                }

                await JsRuntime.Utils().SetStorage("session-accesses", accesses, JavascriptContext.Default.HashSetDateTime, cancellationToken);
            }
        }

        //private async Task ShowOnBoardingPopup(CancellationToken cancellationToken)
        //{
        //    if (Navigation.Uri.Contains("printscreen", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return;
        //    }

        //    var onboarding = await JsRuntime.Utils().GetStorage("onboarding-popup", JavascriptContext.Default.Boolean, cancellationToken);

        //    //show only once
        //    if (!onboarding)
        //    {
        //        await DialogService.OnboardingPopup(Culture);
        //        await JsRuntime.Utils().SetStorage("onboarding-popup", value: true, JavascriptContext.Default.Boolean, cancellationToken);
        //    }
        //}

        protected async Task ShowNotificationError(string message)
        {
            if (!message.CanShowSnackbar()) return;

            Snackbar.Add(message, Severity.Error);

            await JsRuntime.Utils().PlayBeep(220, 400, "square", CancellationToken.None);
            await JsRuntime.Utils().Vibrate([200, 100, 200], CancellationToken.None);
        }

        [JSInvokable]
        public static void ShowError(string error)
        {
            _ = BufferedEvent.Invoke(nameof(ShowError), error);
        }

        [JSInvokable]
        public static void SupabaseAuthChanged(string? token)
        {
            _ = BufferedEvent.Invoke(nameof(SupabaseAuthChanged), token);
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