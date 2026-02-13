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

    private async void OnImportFromExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите Excel-файл",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xlsx" } },
                    { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                    { DevicePlatform.iOS, new[] { "com.microsoft.excel.xlsx" } },
                    { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.spreadsheetml.sheet" } }
                })
            });

            if (result == null)
                return;

            if (!result.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Неверный формат", "Выберите файл в формате .xlsx", "OK");
                return;
            }

            using var stream = await result.OpenReadAsync();
            var importedItems = ReadEquipmentFromExcel(stream).ToList();

            if (importedItems.Count == 0)
            {
                await DisplayAlert("Нет данных", "В файле нет подходящих строк для импорта.", "OK");
                return;
            }

            var existingItems = await _equipmentService.GetAllEquipmentAsync();
            var existingKeys = new HashSet<string>(existingItems.Select(GetUniqueKey), StringComparer.OrdinalIgnoreCase);
            var importedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var importedCount = 0;
            var skippedCount = 0;
            foreach (var item in importedItems)
            {
                var key = GetUniqueKey(item);
                if (existingKeys.Contains(key) || importedKeys.Contains(key))
                {
                    skippedCount++;
                    continue;
                }

                if (await _equipmentService.AddEquipmentAsync(item))
                {
                    importedCount++;
                    existingKeys.Add(key);
                    importedKeys.Add(key);
                }
            }

            await LoadEquipmentAsync();
            await DisplayAlert("Импорт завершён", $"Импортировано: {importedCount}\nПропущено дубликатов: {skippedCount}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось импортировать данные: {ex.Message}", "OK");
        }
    }

    private async void OnEditEquipmentClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Equipment equipment })
            return;

        var type = await DisplayPromptAsync("Редактирование", "Тип", initialValue: equipment.Type);
        if (type is null)
            return;

        var inventoryNumber = await DisplayPromptAsync("Редактирование", "Инвентарный номер", initialValue: equipment.InventoryNumber);
        if (inventoryNumber is null)
            return;

        var office = await DisplayPromptAsync("Редактирование", "Кабинет", initialValue: equipment.Office);
        if (office is null)
            return;

        var status = await DisplayPromptAsync("Редактирование", "Статус", initialValue: equipment.Status);
        if (status is null)
            return;

        var description = await DisplayPromptAsync("Редактирование", "Описание", initialValue: equipment.Description);

        var updatedEquipment = new Equipment
        {
            Id = equipment.Id,
            Type = type.Trim(),
            InventoryNumber = inventoryNumber.Trim(),
            Office = office.Trim(),
            Status = status.Trim(),
            Description = description?.Trim() ?? string.Empty,
            CreatedDate = equipment.CreatedDate
        };

        var success = await _equipmentService.UpdateEquipmentAsync(updatedEquipment);
        if (!success)
        {
            await DisplayAlert("Ошибка", "Не удалось сохранить изменения.", "OK");
            return;
        }

        await LoadEquipmentAsync();
    }

    private async void OnDeleteEquipmentClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Equipment equipment })
            return;

        var confirm = await DisplayAlert(
            "Удаление",
            $"Удалить запись с инвентарным номером '{equipment.InventoryNumber}'?",
            "Да",
            "Нет");

        if (!confirm)
            return;

        var success = await _equipmentService.DeleteEquipmentAsync(equipment.Id);
        if (!success)
        {
            await DisplayAlert("Ошибка", "Не удалось удалить запись.", "OK");
            return;
        }

        await LoadEquipmentAsync();
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

    private static IEnumerable<Equipment> ReadEquipmentFromExcel(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart?.Workbook == null)
            yield break;

        var firstSheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault();
        if (firstSheet == null)
            yield break;

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!);
        var rows = worksheetPart.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().Skip(1) ?? Enumerable.Empty<Row>();

        foreach (var row in rows)
        {
            var values = row.Elements<Cell>().Select(c => GetCellValue(workbookPart, c)).ToList();
            if (values.All(string.IsNullOrWhiteSpace))
                continue;

            yield return new Equipment
            {
                Type = values.ElementAtOrDefault(0)?.Trim() ?? string.Empty,
                InventoryNumber = values.ElementAtOrDefault(1)?.Trim() ?? string.Empty,
                Office = values.ElementAtOrDefault(2)?.Trim() ?? string.Empty,
                Status = values.ElementAtOrDefault(3)?.Trim() ?? string.Empty,
                Description = values.ElementAtOrDefault(4)?.Trim() ?? string.Empty,
                CreatedDate = DateTime.Now
            };
        }
    }

    private static string GetUniqueKey(Equipment item)
    {
        var inventory = item.InventoryNumber?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(inventory))
            return inventory;

        return $"{item.Type?.Trim()}|{item.Office?.Trim()}|{item.Status?.Trim()}|{item.Description?.Trim()}";
    }

    private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        var value = cell.CellValue?.Text ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (stringTable == null)
                return string.Empty;

            if (int.TryParse(value, out var index) && index >= 0 && index < stringTable.ChildElements.Count)
            {
                return stringTable.ChildElements[index].InnerText;
            }
        }

        return value;
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
