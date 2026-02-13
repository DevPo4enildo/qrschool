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

    private async void OnEquipmentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Equipment selectedEquipment)
        {
            return;
        }

        await DisplayAlert(
            "Карточка техники",
            $"Тип: {selectedEquipment.Type}\n" +
            $"Инвентарный номер: {selectedEquipment.InventoryNumber}\n" +
            $"Кабинет: {selectedEquipment.Office}\n" +
            $"Статус: {selectedEquipment.Status}\n" +
            $"Описание: {selectedEquipment.Description}",
            "OK");

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
}
