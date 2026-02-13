using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace qrschool.Models
{
    public class Equipment : INotifyPropertyChanged
    {
        private int _id;
        private string _type;
        private string _office;
        private string _status;
        private string _description;
        private string _inventoryNumber;
        private DateTime _createdDate;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Office
        {
            get => _office;
            set => SetProperty(ref _office, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string InventoryNumber
        {
            get => _inventoryNumber;
            set => SetProperty(ref _inventoryNumber, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}