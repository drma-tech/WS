using Microsoft.AspNetCore.Components;
using MudBlazor;
using WS.WEB.Modules.Help;
using WS.WEB.Shared;

namespace WS.WEB.Core.Helper;

public static class PopupHelper
{
    public static readonly EventCallbackFactory Factory = new();

    public static async Task SettingsPopup(this IDialogService service)
    {
        await service.ShowAsync<SettingsPopup>(Translations.Module.Help.Settings, Options(MaxWidth.Small));
    }

    public static async Task AskReviewPopup(this IDialogService service)
    {
        await service.ShowAsync<AskReview>(Translations.Module.Help.WriteReviewTitle.CustomFormat(AppInfo.Title), Options(MaxWidth.Small, allowClose: false, showHeader: false));
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
            MaxWidth = width,
        };
    }
}
