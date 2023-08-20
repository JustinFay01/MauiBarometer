global using Barometer.Services;
global using Barometer.ViewModel;
using Microsoft.Extensions.Logging;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

//Set windows height and width
//https://stackoverflow.com/questions/72399551/maui-net-set-window-size
namespace Barometer {
    public static class MauiProgram {
        const int WindowWidth = 1366;
        const int WindowHeight = 768;

        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) => {
#if WINDOWS
            var mauiWindow = handler.VirtualView;
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
#endif
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<SerialPortController>();
            builder.Services.AddSingleton<BarometerViewModel>();
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}