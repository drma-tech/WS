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
        await service.ShowAsync<AskReview>(string.Format("Want to help {0} grow?", SeoTranslations.AppName), Options(MaxWidth.Small));
    }

    public static DialogOptions Options(MaxWidth width)
    {
        return new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            Position = DialogPosition.Center,
            MaxWidth = width
        };
    }
}