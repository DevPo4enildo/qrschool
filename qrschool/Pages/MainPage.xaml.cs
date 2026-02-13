namespace qrschool.Pages;

public partial class MainPage : ContentPage
{
    private readonly IServiceProvider _services;

    public MainPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var scanPage = _services.GetRequiredService<ScanPage>();
        await Navigation.PushAsync(scanPage);
    }

    private async void OnCRUDPage(object sender, EventArgs e)
    {
        var crudPage = _services.GetRequiredService<CRUDPage>();
        await Navigation.PushAsync(crudPage);
    }

    private async void OnEquipmentListPage(object sender, EventArgs e)
    {
        var equipmentListPage = _services.GetRequiredService<EquipmentListPage>();
        await Navigation.PushAsync(equipmentListPage);
    }
}
