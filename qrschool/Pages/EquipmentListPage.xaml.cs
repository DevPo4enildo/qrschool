using System.Collections.ObjectModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using qrschool.Models;
using qrschool.Services;

namespace qrschool.Pages;

public partial class EquipmentListPage : ContentPage
{
    private readonly IEquipmentService _equipmentService;

    public ObservableCollection<Equipment> EquipmentItems { get; } = new();

    public EquipmentListPage(IEquipmentService equipmentService)
    {
        InitializeComponent();
        _equipmentService = equipmentService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEquipmentAsync();
    }

    private async Task LoadEquipmentAsync()
    {
        var items = await _equipmentService.GetAllEquipmentAsync();

        EquipmentItems.Clear();
        foreach (var item in items.OrderByDescending(x => x.CreatedDate))
        {
            EquipmentItems.Add(item);
        }
    }

    private async void OnExportToExcelClicked(object sender, EventArgs e)
    {
        if (EquipmentItems.Count == 0)
        {
            await DisplayAlert("Нет данных", "Список техники пуст, выгружать нечего.", "OK");
            return;
        }

        try
        {
            var fileName = $"equipment-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            CreateExcelFile(filePath, EquipmentItems);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Выгрузка техники",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось создать Excel-файл: {ex.Message}", "OK");
        }
    }

    private static void CreateExcelFile(string filePath, IEnumerable<Equipment> items)
    {
        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        worksheetPart.Worksheet = new Worksheet(sheetData);

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        var sheet = new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Техника"
        };
        sheets.Append(sheet);

        sheetData.Append(CreateRow("Тип", "Инвентарный номер", "Кабинет", "Статус", "Описание", "Дата добавления"));

        foreach (var item in items)
        {
            sheetData.Append(CreateRow(
                item.Type,
                item.InventoryNumber,
                item.Office,
                item.Status,
                item.Description,
                item.CreatedDate.ToString("yyyy-MM-dd HH:mm")));
        }

        workbookPart.Workbook.Save();
    }

    private static Row CreateRow(params string[] values)
    {
        var row = new Row();

        foreach (var value in values)
        {
            row.Append(new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value ?? string.Empty)
            });
        }

        return row;
    }
}
