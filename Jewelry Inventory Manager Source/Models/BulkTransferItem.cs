using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class BulkTransferItem : INotifyPropertyChanged
    {
        private int _id;
        private int _transferOrderId;
        private int _itemId;
        private string _itemType; // "Product" or "Component"
        private string _itemName;
        private string _sku;
        private int _quantity;
        private int _availableStock;
        private string _notes;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public int TransferOrderId
        {
            get => _transferOrderId;
            set
            {
                _transferOrderId = value;
                OnPropertyChanged(nameof(TransferOrderId));
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

        public int AvailableStock
        {
            get => _availableStock;
            set
            {
                _availableStock = value;
                OnPropertyChanged(nameof(AvailableStock));
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 