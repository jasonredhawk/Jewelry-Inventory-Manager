using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class TransferItem : INotifyPropertyChanged
    {
        private int _itemId;
        private string _itemName;
        private string _itemType;
        private string _sku;
        private int _quantity;
        private string _fromLocation;
        private string _toLocation;
        private int _currentStock;

        public int ItemId
        {
            get => _itemId;
            set
            {
                _itemId = value;
                OnPropertyChanged(nameof(ItemId));
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

        public string ItemType
        {
            get => _itemType;
            set
            {
                _itemType = value;
                OnPropertyChanged(nameof(ItemType));
            }
        }

        public string SKU
        {
            get => _sku;
            set
            {
                _sku = value;
                OnPropertyChanged(nameof(SKU));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public string FromLocation
        {
            get => _fromLocation;
            set
            {
                _fromLocation = value;
                OnPropertyChanged(nameof(FromLocation));
            }
        }

        public string ToLocation
        {
            get => _toLocation;
            set
            {
                _toLocation = value;
                OnPropertyChanged(nameof(ToLocation));
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 