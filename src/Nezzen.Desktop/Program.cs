using Avalonia;
namespace Nezzen.Desktop;

internal static class Program
{
    [STAThread] public static int Main(string[] args) => AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
