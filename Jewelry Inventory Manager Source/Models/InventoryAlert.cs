using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class InventoryAlert : INotifyPropertyChanged
    {
        private int _id;
        private DateTime _alertDate;
        private string _alertType;
        private int _itemId;
        private string _itemType;
        private string _itemName;
        private string _locationName;
        private int _currentStock;
        private int _minimumStock;
        private string _message;
        private bool _isDismissed;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public DateTime AlertDate
        {
            get => _alertDate;
            set
            {
                _alertDate = value;
                OnPropertyChanged(nameof(AlertDate));
            }
        }

        public string AlertType
        {
            get => _alertType;
            set
            {
                _alertType = value;
                OnPropertyChanged(nameof(AlertType));
            }
        }

        public int ItemId
        {
            get => _itemId;
            set
            {
                _itemId = value;
                OnPropertyChanged(nameof(ItemId));
            }
        }

        public string ItemType
        {
            get => _itemType;
            set
            {
                _itemType = value;
                OnPropertyChanged(nameof(ItemType));
            }
        }

        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        public string LocationName
        {
            get => _locationName;
            set
            {
                _locationName = value;
                OnPropertyChanged(nameof(LocationName));
            }
        }

        public int CurrentStock
        {
            get => _currentStock;
            set
            {
                _currentStock = value;
                OnPropertyChanged(nameof(CurrentStock));
            }
        }

        public int MinimumStock
        {
            get => _minimumStock;
            set
            {
                _minimumStock = value;
                OnPropertyChanged(nameof(MinimumStock));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public bool IsDismissed
        {
            get => _isDismissed;
            set
            {
                _isDismissed = value;
                OnPropertyChanged(nameof(IsDismissed));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 