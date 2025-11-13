using Microsoft.Extensions.Logging;
using qrschool.Models;
using qrschool.Pages;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace qrschool
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            string connectionString = "Host=localhost;Port=5432;Database=qr;Username=postgres;Password=";
            builder.Services.AddSingleton(new InventoryRepository(connectionString));

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ScanPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
