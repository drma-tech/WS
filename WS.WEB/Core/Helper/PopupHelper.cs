using Microsoft.AspNetCore.Components;
using MudBlazor;
using WS.WEB.Modules.Support;
using WS.WEB.Resources;
using WS.WEB.Shared;

namespace WS.WEB.Core.Helper;

public static class PopupHelper
{
    public static readonly EventCallbackFactory Factory = new();

    public static async Task SettingsPopup(this IDialogService service)
    {
        await service.ShowAsync<SettingsPopup>("Settings", Options(MaxWidth.Small));
    }

    public static async Task AskReviewPopup(this IDialogService service)
    {
        await service.ShowAsync<AskReview>(string.Format(GlobalTranslations.WriteReviewTitle, SeoTranslations.AppName), Options(MaxWidth.Small, false, false));
    }

    public static DialogOptions Options(MaxWidth width, bool allowClose = true, bool showHeader = true)
    {
        return new DialogOptions
        {
            CloseOnEscapeKey = allowClose,
            CloseButton = allowClose,
            BackdropClick = allowClose,
            NoHeader = !showHeader,
            Position = DialogPosition.Center,
            MaxWidth = width
        };
    }
}
