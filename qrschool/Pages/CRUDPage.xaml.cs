using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using qrschool.Models;
using qrschool.Services;

namespace qrschool.Pages;

public partial class CRUDPage : ContentPage
{
    public CRUDPage()
    {
        InitializeComponent();
        BindingContext = new AddEquipmentViewModel(new MockEquipmentService());
    }

    public partial class AddEquipmentViewModel : ObservableObject
    {
        private readonly IEquipmentService _equipmentService;

        [ObservableProperty]
        private Equipment _equipment;

        [ObservableProperty]
        private ObservableCollection<string> _equipmentTypes;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _selectedType;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddEquipmentViewModel(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
            Equipment = new Equipment();

            EquipmentTypes = new ObservableCollection<string>();

            SaveCommand = new AsyncRelayCommand(OnSaveAsync);
            CancelCommand = new AsyncRelayCommand(OnCancelAsync);

            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                var types = await _equipmentService.GetEquipmentTypesAsync();

                EquipmentTypes.Clear();
                foreach (var type in types)
                    EquipmentTypes.Add(type);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnSaveAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Equipment.Type))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Выберите тип техники", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Equipment.Office))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Введите кабинет", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Equipment.Status))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Введите статус", "OK");
                    return;
                }

                IsBusy = true;

                bool result = await _equipmentService.AddEquipmentAsync(Equipment);

                if (result)
                {
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Техника успешно добавлена!", "OK");

                    Equipment = new Equipment();
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось добавить технику", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка сохранения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnCancelAsync()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Подтверждение",
                "Отменить добавление техники? Все несохранённые данные будут потеряны.",
                "Да, отменить",
                "Нет, остаться");

            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        partial void OnSelectedTypeChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
                Equipment.Type = value;
        }
    }
}
