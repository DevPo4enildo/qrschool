using qrschool.Service;
using ZXing.Net.Maui;

namespace qrschool.Pages;

public partial class ScanPage : ContentPage
{
    private readonly InventoryRepository _repo;
    private bool _isProcessing;

    public ScanPage(InventoryRepository repo)
    {
        InitializeComponent();
        _repo = repo;
    }

    private async void CameraView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;

        var result = e.Results?.FirstOrDefault();
        if (result == null) return;

        var code = result.Value?.Trim();
        if (string.IsNullOrEmpty(code)) return;

        _isProcessing = true;

        var item = await _repo.GetByCodeAsync(code);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (item == null)
            {
                TypeLabel.Text = "Тип: —";
                RoomLabel.Text = "Кабинет: —";
                StatusLabel.Text = "Не найдено";
                DescriptionLabel.Text = "";
            }
            else
            {
                TypeLabel.Text = $"Тип: {item.ObjectType}";
                RoomLabel.Text = $"Кабинет: {item.RoomName ?? "—"}";
                StatusLabel.Text = $"Статус: {item.Status}";
                DescriptionLabel.Text = item.Description ?? "—";
            }
        });

        await Task.Delay(1500);
        _isProcessing = false;
    }
}
