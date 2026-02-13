using System.Collections.ObjectModel;
using System.Globalization;
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

    private async void OnImportFromExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите Excel файл",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] },
                    { DevicePlatform.iOS, ["com.microsoft.excel.xlsx"] },
                    { DevicePlatform.WinUI, [".xlsx"] },
                    { DevicePlatform.macOS, ["xlsx"] }
                })
            });

            if (result == null)
            {
                return;
            }

            var importedItems = ReadEquipmentFromExcel(result.FullPath);
            if (importedItems.Count == 0)
            {
                await DisplayAlert("Импорт", "В Excel не найдено строк для загрузки.", "OK");
                return;
            }

            var addedCount = 0;
            foreach (var item in importedItems)
            {
                if (await _equipmentService.AddEquipmentAsync(item))
                {
                    addedCount++;
                }
            }

            await LoadEquipmentAsync();
            await DisplayAlert("Импорт завершён", $"Успешно добавлено записей: {addedCount}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить данные из Excel: {ex.Message}", "OK");
        }
    }

    private async void OnEditEquipmentClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Equipment equipment })
        {
            return;
        }

        var edited = await PromptForValue("Редактирование", "Тип техники", equipment.Type);
        if (edited == null) return;
        equipment.Type = edited;

        edited = await PromptForValue("Редактирование", "Инвентарный номер", equipment.InventoryNumber);
        if (edited == null) return;
        equipment.InventoryNumber = edited;

        edited = await PromptForValue("Редактирование", "Кабинет", equipment.Office);
        if (edited == null) return;
        equipment.Office = edited;

        edited = await PromptForValue("Редактирование", "Статус", equipment.Status);
        if (edited == null) return;
        equipment.Status = edited;

        edited = await PromptForValue("Редактирование", "Описание", equipment.Description ?? string.Empty);
        if (edited == null) return;
        equipment.Description = edited;

        var updated = await _equipmentService.UpdateEquipmentAsync(equipment);
        if (updated)
        {
            await DisplayAlert("Успех", "Данные техники обновлены.", "OK");
            await LoadEquipmentAsync();
        }
        else
        {
            await DisplayAlert("Ошибка", "Не удалось обновить запись.", "OK");
        }
    }

    private async Task<string?> PromptForValue(string title, string fieldName, string initialValue)
    {
        return await DisplayPromptAsync(title, fieldName, "OK", "Отмена", initialValue: initialValue);
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

    private static List<Equipment> ReadEquipmentFromExcel(string filePath)
    {
        var result = new List<Equipment>();

        using var document = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart?.Workbook.Sheets == null)
        {
            return result;
        }

        var firstSheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();
        if (firstSheet == null)
        {
            return result;
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        if (sheetData == null)
        {
            return result;
        }

        foreach (var row in sheetData.Elements<Row>().Skip(1))
        {
            var cells = row.Elements<Cell>().ToArray();
            if (cells.Length < 5)
            {
                continue;
            }

            var equipment = new Equipment
            {
                Type = GetCellValue(workbookPart, cells[0]),
                InventoryNumber = GetCellValue(workbookPart, cells[1]),
                Office = GetCellValue(workbookPart, cells[2]),
                Status = GetCellValue(workbookPart, cells[3]),
                Description = GetCellValue(workbookPart, cells[4])
            };

            var rawDate = cells.Length > 5 ? GetCellValue(workbookPart, cells[5]) : null;
            if (DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                equipment.CreatedDate = parsedDate;
            }

            if (!string.IsNullOrWhiteSpace(equipment.Type) &&
                !string.IsNullOrWhiteSpace(equipment.InventoryNumber) &&
                !string.IsNullOrWhiteSpace(equipment.Office) &&
                !string.IsNullOrWhiteSpace(equipment.Status))
            {
                result.Add(equipment);
            }
        }

        return result;
    }

    private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        var value = cell.CellValue?.InnerText ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            return workbookPart.SharedStringTablePart?.SharedStringTable
                .ElementAt(int.Parse(value)).InnerText ?? string.Empty;
        }

        return value;
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
