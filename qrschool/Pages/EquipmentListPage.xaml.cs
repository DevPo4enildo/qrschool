using System.Collections.ObjectModel;
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
}
