using WS.WEB.Core.Helper;

namespace WS.WEB.Core;

public static class AppStateStatic
{
    [Custom(Name = "Dark Mode")]
    public static bool DarkMode { get; private set; }

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