using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using qrschool.Models;
using qrschool.Services;

namespace qrschool.Pages;

public partial class CRUDPage : ContentPage
{
    public CRUDPage(IEquipmentService equipmentService)
    {
        InitializeComponent();
        BindingContext = new AddEquipmentViewModel(equipmentService);
    }

    public partial class AddEquipmentViewModel : ObservableObject
    {
        private readonly IEquipmentService _equipmentService;

        [ObservableProperty]
        private Equipment _equipment = new();

        [ObservableProperty]
        private ObservableCollection<string> _equipmentTypes = new();

        [ObservableProperty]
        private ObservableCollection<string> _statusOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> _officeList = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _selectedType;

        [ObservableProperty]
        private string? _selectedOffice;

        [ObservableProperty]
        private string? _selectedStatus;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddNewOfficeCommand { get; }

        public AddEquipmentViewModel(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
            Equipment = new Equipment();

            EquipmentTypes = new ObservableCollection<string>();
            StatusOptions = new ObservableCollection<string>();
            OfficeList = new ObservableCollection<string>();

            SaveCommand = new AsyncRelayCommand(OnSaveAsync);
            CancelCommand = new AsyncRelayCommand(OnCancelAsync);
            AddNewOfficeCommand = new AsyncRelayCommand(OnAddNewOfficeAsync);

            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                var types = await _equipmentService.GetEquipmentTypesAsync();
                var offices = await _equipmentService.GetOfficesAsync();
                var statuses = await _equipmentService.GetStatusOptionsAsync();

                EquipmentTypes.Clear();
                foreach (var type in types)
                    EquipmentTypes.Add(type);

                OfficeList.Clear();
                foreach (var office in offices)
                    OfficeList.Add(office);

                StatusOptions.Clear();
                foreach (var status in statuses)
                    StatusOptions.Add(status);
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Ошибка",
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
                // Валидация
                if (string.IsNullOrWhiteSpace(Equipment.Type))
                {
                    await Application.Current!.MainPage!.DisplayAlert("Ошибка",
                        "Укажите тип техники", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Equipment.Office))
                {
                    await Application.Current!.MainPage!.DisplayAlert("Ошибка",
                        "Укажите кабинет", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Equipment.Status))
                {
                    await Application.Current!.MainPage!.DisplayAlert("Ошибка",
                        "Укажите статус", "OK");
                    return;
                }

                IsBusy = true;

                // Сохранение в базу
                bool result = await _equipmentService.AddEquipmentAsync(Equipment);

                if (result)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Успех",
                        "Техника успешно добавлена!", "OK");

                    // Очистка формы
                    Equipment = new Equipment();

                    // Возврат на предыдущую страницу
                    await Application.Current!.MainPage!.Navigation.PopAsync();
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Ошибка",
                        "Не удалось сохранить технику", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Ошибка",
                    $"Ошибка сохранения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnCancelAsync()
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Подтверждение",
                "Отменить добавление техники? Все несохранённые данные будут потеряны.",
                "Да, отменить",
                "Нет, продолжить");

            if (confirm)
            {
                await Application.Current!.MainPage!.Navigation.PopAsync();
            }
        }

        private async Task OnAddNewOfficeAsync()
        {
            string? newOffice = await Application.Current!.MainPage!.DisplayPromptAsync(
                "Новый кабинет",
                "Введите номер кабинета:",
                "Добавить",
                "Отмена",
                "Например: 405",
                -1,
                Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(newOffice))
            {
                if (!OfficeList.Contains(newOffice))
                {
                    OfficeList.Add(newOffice);

                    // Сортируем список кабинетов
                    var sorted = OfficeList.OrderBy(o => o).ToList();
                    OfficeList.Clear();
                    foreach (var office in sorted)
                        OfficeList.Add(office);

                    Equipment.Office = newOffice;
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Внимание",
                        "Такой кабинет уже существует", "OK");
                }
            }
        }

        // Методы для отслеживания изменений в выпадающих списках
        partial void OnSelectedTypeChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                Equipment.Type = value;
        }

        partial void OnSelectedOfficeChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                Equipment.Office = value;
        }

        partial void OnSelectedStatusChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                Equipment.Status = value;
        }
    }
}