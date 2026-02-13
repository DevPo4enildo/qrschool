using System.Text;
using qrschool.Service;
using ZXing.Net.Maui;

namespace qrschool.Pages;

public partial class ScanPage : ContentPage
{
    private readonly InventoryRepository _repo;
    private bool _isProcessing;
    private string? _lastScannedCode;

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
            _lastScannedCode = code;
            ExportButton.IsEnabled = true;

            if (item == null)
            {
                TypeLabel.Text = "Тип: -";
                RoomLabel.Text = "Кабинет: -";
                StatusLabel.Text = "Не найдено";
                DescriptionLabel.Text = "";
            }
            else
            {
                TypeLabel.Text = $"Тип: {item.ObjectType}";
                RoomLabel.Text = $"Кабинет: {item.RoomName ?? "-"}";
                StatusLabel.Text = $"Статус: {item.Status}";
                DescriptionLabel.Text = $"Описание: {item.Description ?? "-"}";
            }
        });

        await Task.Delay(1500);
        _isProcessing = false;
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastScannedCode))
        {
            await DisplayAlert("Ошибка", "Сначала отсканируйте QR-код.", "OK");
            return;
        }

        var item = await _repo.GetByCodeAsync(_lastScannedCode);
        if (item == null)
        {
            await DisplayAlert("Ошибка", "Оборудование не найдено.", "OK");
            return;
        }

        try
        {
            var exportsDirectory = FileSystem.AppDataDirectory;
            var fileName = $"inventory_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(exportsDirectory, fileName);

            var csv = BuildCsv(item);
            await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Экспорт в Excel",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось выгрузить файл: {ex.Message}", "OK");
        }
    }

    private static string BuildCsv(qrschool.Models.InventoryItemDto item)
    {
        static string Escape(string? value)
        {
            var safe = value ?? string.Empty;
            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Код,Тип,Инвентарный номер,Кабинет,Статус,Описание");
        sb.AppendLine(string.Join(",", new[]
        {
            Escape(item.Code),
            Escape(item.ObjectType),
            Escape(item.InventoryNo),
            Escape(item.RoomName),
            Escape(item.Status),
            Escape(item.Description)
        }));

        return sb.ToString();
    }
}
