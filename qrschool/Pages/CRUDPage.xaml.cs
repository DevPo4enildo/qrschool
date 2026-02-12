using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qrschool.Models;
using qrschool.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace qrschool.Pages;

public partial class CRUDPage : ContentPage
{
    public CRUDPage(AddEquipmentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public partial class AddEquipmentViewModel : ObservableObject
    {
        private readonly IEquipmentService _equipmentService;

        [ObservableProperty]
        private Equipment _equipment = new();

        [ObservableProperty]
        private Equipment? _selectedEquipment;

        [ObservableProperty]
        private ObservableCollection<Equipment> _equipmentItems = new();

        [ObservableProperty]
        private ObservableCollection<string> _equipmentTypes = new();

        [ObservableProperty]
        private ObservableCollection<string> _statusOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> _officeList = new();

        [ObservableProperty]
        private bool _isBusy;

        public ICommand SaveCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }

        public AddEquipmentViewModel(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;

            SaveCommand = new AsyncRelayCommand(OnSaveAsync);
            UpdateCommand = new AsyncRelayCommand(OnUpdateAsync);
            DeleteCommand = new AsyncRelayCommand(OnDeleteAsync);
            ClearCommand = new RelayCommand(ClearForm);

            _ = LoadDataAsync();
        }

        partial void OnSelectedEquipmentChanged(Equipment? value)
        {
            if (value == null)
            {
                return;
            }

            Equipment = new Equipment
            {
                Id = value.Id,
                Type = value.Type,
                Office = value.Office,
                Status = value.Status,
                Description = value.Description,
                CreatedDate = value.CreatedDate
            };
        }

        private async Task LoadDataAsync()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;

                var types = await _equipmentService.GetEquipmentTypesAsync();
                var offices = await _equipmentService.GetOfficesAsync();
                var statuses = await _equipmentService.GetStatusOptionsAsync();
                var items = await _equipmentService.GetAllEquipmentAsync();

                EquipmentTypes = new ObservableCollection<string>(types);
                OfficeList = new ObservableCollection<string>(offices);
                StatusOptions = new ObservableCollection<string>(statuses);
                EquipmentItems = new ObservableCollection<Equipment>(items);
            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnSaveAsync()
        {
            if (!ValidateForm())
            {
                return;
            }

            Equipment.CreatedDate = DateTime.UtcNow;
            var created = await _equipmentService.AddEquipmentAsync(Equipment);
            if (!created)
            {
                await ShowAlert("Ошибка", "Не удалось создать запись");
                return;
            }

            await LoadDataAsync();
            ClearForm();
        }

        private async Task OnUpdateAsync()
        {
            if (Equipment.Id <= 0)
            {
                await ShowAlert("Ошибка", "Выберите запись для обновления");
                return;
            }

            if (!ValidateForm())
            {
                return;
            }

            var updated = await _equipmentService.UpdateEquipmentAsync(Equipment);
            if (!updated)
            {
                await ShowAlert("Ошибка", "Не удалось обновить запись");
                return;
            }

            await LoadDataAsync();
            ClearForm();
        }

        private async Task OnDeleteAsync()
        {
            if (Equipment.Id <= 0)
            {
                await ShowAlert("Ошибка", "Выберите запись для удаления");
                return;
            }

            var deleted = await _equipmentService.DeleteEquipmentAsync(Equipment.Id);
            if (!deleted)
            {
                await ShowAlert("Ошибка", "Не удалось удалить запись");
                return;
            }

            await LoadDataAsync();
            ClearForm();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Equipment.Type) ||
                string.IsNullOrWhiteSpace(Equipment.Office) ||
                string.IsNullOrWhiteSpace(Equipment.Status))
            {
                _ = ShowAlert("Проверка", "Заполните тип, кабинет и статус");
                return false;
            }

            if (!OfficeList.Contains(Equipment.Office))
            {
                OfficeList.Add(Equipment.Office);
            }

            return true;
        }

        private void ClearForm()
        {
            SelectedEquipment = null;
            Equipment = new Equipment();
        }

        private static Task ShowAlert(string title, string message)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "OK");
                }
            });
        }
    }
}
