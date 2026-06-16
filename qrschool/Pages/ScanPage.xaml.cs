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
        if (_isProcessing)
            return;

        var result = e.Results?.FirstOrDefault();
        var code = result?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return;

        _isProcessing = true;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CodeLabel.Text = $"Код: {code}";
                StatusLabel.Text = "Идёт поиск...";
            });

            var item = await _repo.GetByCodeAsync(code);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrWhiteSpace(_repo.LastError))
                {
                    TypeLabel.Text = "Тип: —";
                    RoomLabel.Text = "Кабинет: —";
                    StatusLabel.Text = "Ошибка подключения к базе";
                    DescriptionLabel.Text = _repo.LastError;
                    return;
                }

                if (item == null)
                {
                    TypeLabel.Text = "Тип: —";
                    RoomLabel.Text = "Кабинет: —";
                    StatusLabel.Text = "Не найдено";
                    DescriptionLabel.Text = "Описание: —";
                }
                else
                {
                    TypeLabel.Text = $"Тип: {item.ObjectType}";
                    RoomLabel.Text = $"Кабинет: {item.RoomName ?? "—"}";
                    StatusLabel.Text = $"Статус: {item.Status}";
                    DescriptionLabel.Text = $"Описание: {item.Description ?? "—"}";
                }
            });
        }
        finally
        {
            await Task.Delay(1500);
            _isProcessing = false;
        }
    }
}
