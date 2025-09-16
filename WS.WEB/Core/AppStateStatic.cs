using MudBlazor;
using WS.Shared.Enums;
using WS.WEB.Core.Helper;

namespace WS.WEB.Core;

public static class AppStateStatic
{
    public static Breakpoint Breakpoint { get; set; }
    public static Action<Breakpoint>? BreakpointChanged { get; set; }

    [Custom(Name = "Dark Mode")]
    public static bool DarkMode { get; private set; }

    public static Platform Platform { get; set; } = Platform.webapp;
    public static string? Version { get; set; }

    public static Action? DarkModeChanged { get; set; }
    public static Action<string>? ShowError { get; set; }
    public static Action? ProcessingStarted { get; set; }
    public static Action? ProcessingFinished { get; set; }

    public static void ChangeDarkMode(bool darkMode)
    {
        DarkMode = darkMode;
        DarkModeChanged?.Invoke();
    }
}