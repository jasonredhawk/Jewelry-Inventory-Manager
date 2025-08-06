using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class PurchaseOrderItem : INotifyPropertyChanged
    {
        private int _id;
        private int _purchaseOrderId;
        private string _itemType;
        private int _itemId;
        private int _locationId;
        private int _quantityOrdered;
        private int _quantityReceived;
        private decimal _unitCost;
        private decimal _totalCost;
        private string _notes;
        private string _itemName;
        private string _itemSku;
        private string _locationName;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public int PurchaseOrderId
        {
            get => _purchaseOrderId;
            set
            {
                _purchaseOrderId = value;
                OnPropertyChanged(nameof(PurchaseOrderId));
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

        public int ItemId
        {
            get => _itemId;
            set
            {
                _itemId = value;
                OnPropertyChanged(nameof(ItemId));
            }
        }

        public int LocationId
        {
            get => _locationId;
            set
            {
                _locationId = value;
                OnPropertyChanged(nameof(LocationId));
            }
        }

        public int QuantityOrdered
        {
            get => _quantityOrdered;
            set
            {
                _quantityOrdered = value;
                OnPropertyChanged(nameof(QuantityOrdered));
            }
        }

        public int QuantityReceived
        {
            get => _quantityReceived;
            set
            {
                _quantityReceived = value;
                OnPropertyChanged(nameof(QuantityReceived));
            }
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                _unitCost = value;
                OnPropertyChanged(nameof(UnitCost));
            }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set
            {
                _totalCost = value;
                OnPropertyChanged(nameof(TotalCost));
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

        // Navigation properties for display
        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        public string ItemSku
        {
            get => _itemSku;
            set
            {
                _itemSku = value;
                OnPropertyChanged(nameof(ItemSku));
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 