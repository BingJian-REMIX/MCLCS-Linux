using Avalonia;

namespace MCLCS.Linux.App;

internal static class Program
{
    // 标准 Avalonia 桌面入口（Linux/X11/Wayland）
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
