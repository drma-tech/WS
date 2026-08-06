using Microsoft.AspNetCore.Components;

namespace WS.WEB.Shared
{
    public partial class RenderControl<T> where T : class
    {
        [Parameter][EditorRequired] public RenderControlState<T> Actions { get; set; } = null!;
        [Parameter][EditorRequired] public RenderFragment<T> ChildContent { get; set; } = null!;

        [Parameter] public string? Class { get; set; }
        [Parameter] public string? LoadingHeight { get; set; } = "230px";

        [Parameter] public bool PrivateFeature { get; set; }
        [Parameter] public bool IsAuthenticated { get; set; }

        [Parameter] public bool PremiumFeature { get; set; }
        [Parameter] public bool IsPremium { get; set; }

        [Parameter] public bool HideIfEmpty { get; set; }

        protected override void OnInitialized()
        {
            Actions.OnStateChanged += StateHasChanged;
        }
    }
}