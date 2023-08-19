using Microsoft.Extensions.Logging;
using Barometer.Services;
using Barometer.ViewModel;

namespace Barometer {
    public static class MauiProgram {
        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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