using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WS.WEB.Core;

/// <summary>
///     if you implement the OnAfterRenderAsync method, call 'await base.OnAfterRenderAsync(firstRender);'
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ComponentCore<T> : ComponentBase, IBrowserViewportObserver, IAsyncDisposable where T : class
{
    [Inject] protected ILogger<T> Logger { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = null!;
    public Breakpoint Breakpoint { get; set; }

    /// <summary>
    /// Mandatory data to fill out the page/component without delay (essential for bots, SEO, etc.)
    /// </summary>
    /// <returns></returns>
    protected virtual Task LoadEssentialDataAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Non-critical data that may be delayed (popups, javascript handling, authenticated user data, etc.)
    /// </summary>
    /// <returns></returns>
    protected virtual Task LoadNonEssentialDataAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadEssentialDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
                await LoadNonEssentialDataAsync();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            ex.ProcessException(Snackbar, Logger);
        }
    }

    #region BrowserViewportObserver

    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        Breakpoint = browserViewportEventArgs.Breakpoint;

        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);
        GC.SuppressFinalize(this);
    }

    #endregion BrowserViewportObserver
}

/// <summary>
///     if you implement the OnAfterRenderAsync method, call 'await base.OnAfterRenderAsync(firstRender);'
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class PageCore<T> : ComponentCore<T> where T : class
{
}