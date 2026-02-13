using qrschool.Models;

namespace qrschool.Services
{
    public interface IEquipmentService
    {
        Task<bool> AddEquipmentAsync(Equipment equipment);
        Task<List<Equipment>> GetAllEquipmentAsync();
        Task<Equipment> GetEquipmentByIdAsync(int id);
        Task<bool> UpdateEquipmentAsync(Equipment equipment);
        Task<bool> DeleteEquipmentAsync(int id);
        Task<List<string>> GetEquipmentTypesAsync();
        Task<List<string>> GetOfficesAsync();
        Task<List<string>> GetStatusOptionsAsync();
    }

    public class MockEquipmentService : IEquipmentService
    {
        private List<Equipment> _mockDatabase;
        private int _nextId = 1;

        public MockEquipmentService()
        {
            InitializeMockData();
        }

        private void InitializeMockData()
        {
            _mockDatabase = new List<Equipment>();
        }

        public async Task<bool> AddEquipmentAsync(Equipment equipment)
        {
            try
            {
                await Task.Delay(300); // Имитация задержки сети

                equipment.Id = _nextId++;
                equipment.CreatedDate = DateTime.Now;
                _mockDatabase.Add(equipment);

                Console.WriteLine($"Добавлена техника: {equipment.Type} в кабинет {equipment.Office}");
                Console.WriteLine($"Всего записей: {_mockDatabase.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Equipment>> GetAllEquipmentAsync()
        {
            await Task.Delay(200); // Имитация задержки
            return new List<Equipment>(_mockDatabase);
        }

        public async Task<Equipment> GetEquipmentByIdAsync(int id)
        {
            await Task.Delay(100);
            return _mockDatabase.FirstOrDefault(e => e.Id == id);
        }

        public async Task<bool> UpdateEquipmentAsync(Equipment equipment)
        {
            await Task.Delay(300);
            var existing = _mockDatabase.FirstOrDefault(e => e.Id == equipment.Id);
            if (existing != null)
            {
                existing.Type = equipment.Type;
                existing.InventoryNumber = equipment.InventoryNumber;
                existing.Office = equipment.Office;
                existing.Status = equipment.Status;
                existing.Description = equipment.Description;
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteEquipmentAsync(int id)
        {
            await Task.Delay(300);
            var item = _mockDatabase.FirstOrDefault(e => e.Id == id);
            if (item != null)
            {
                _mockDatabase.Remove(item);
                return true;
            }
            return false;
        }

        public async Task<List<string>> GetEquipmentTypesAsync()
        {
            await Task.Delay(100);
            return new List<string>
            {
                "Компьютер",
                "Монитор",
                "Принтер",
                "Сканер",
                "Ноутбук",
                "Проектор",
                "ИБП",
                "Сетевое оборудование",
                "Телефон",
                "МФУ"
            };
        }

        public async Task<List<string>> GetOfficesAsync()
        {
            await Task.Delay(100);
            return new List<string>
            {
                "101", "102", "103",
                "201", "202", "203",
                "301", "302", "303",
                "401", "402", "404"
            };
        }

        public async Task<List<string>> GetStatusOptionsAsync()
        {
            await Task.Delay(100);
            return new List<string>
            {
                "Рабочий",
                "В ремонте",
                "Списано",
                "Резерв",
                "Новый",
                "Требует настройки"
            };
        }
    }
}