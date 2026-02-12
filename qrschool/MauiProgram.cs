using Microsoft.Extensions.Logging;
using qrschool.Pages;
using qrschool.Service;
using qrschool.Services;
using ZXing.Net.Maui;

namespace qrschool;

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

        const string connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123";

        builder.Services.AddSingleton<InventoryRepository>(_ => new InventoryRepository(connectionString));
        builder.Services.AddSingleton<IEquipmentService>(_ => new EquipmentService(connectionString));
        builder.Services.AddTransient<CRUDPage.AddEquipmentViewModel>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<CRUDPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
