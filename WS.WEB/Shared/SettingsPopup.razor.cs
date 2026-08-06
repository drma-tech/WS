using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WS.WEB.Shared
{
    public partial class SettingsPopup
    {
        [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

        public bool DarkMode { get; set; }

        protected override async Task<bool> LoadInteropDataAsync(Microsoft.JSInterop.IJSRuntime JsRuntime)
        {
            DarkMode = await AppStateStatic.GetDarkMode(JsRuntime, Cts.Token) ?? false;
            return true;
        }

        protected async Task DarkModeChanged(bool value)
        {
            DarkMode = value;

            await JsRuntime.Utils().SetStorage("dark-mode", value, JavascriptContext.Default.Boolean, Cts.Token);

            AppStateStatic.ChangeDarkMode(value);
        }
    }
}