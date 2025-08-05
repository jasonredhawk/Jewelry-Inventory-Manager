using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class InventoryItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _type;
        private string _sku;
        private string _description;
        private int _currentStock;
        private int _minimumStock;
        private decimal _price;
        private decimal _cost;
        private bool _isActive;
        private DateTime _lastModified;
        private string _stockStatus;
        private bool _isSelected;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        // Alias for Type for compatibility
        public string ItemType
        {
            get => Type;
            set => Type = value;
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

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public int CurrentStock
        {
            get => _currentStock;
            set
            {
                _currentStock = value;
                UpdateStockStatus();
                OnPropertyChanged(nameof(CurrentStock));
            }
        }

        public int MinimumStock
        {
            get => _minimumStock;
            set
            {
                _minimumStock = value;
                UpdateStockStatus();
                OnPropertyChanged(nameof(MinimumStock));
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }

        public decimal Cost
        {
            get => _cost;
            set
            {
                _cost = value;
                OnPropertyChanged(nameof(Cost));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged(nameof(LastModified));
            }
        }

        public string StockStatus
        {
            get => _stockStatus;
            set
            {
                _stockStatus = value;
                OnPropertyChanged(nameof(StockStatus));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private void UpdateStockStatus()
        {
            if (CurrentStock <= 0)
            {
                StockStatus = "Out of Stock";
            }
            else if (CurrentStock <= MinimumStock)
            {
                StockStatus = "Low Stock";
            }
            else
            {
                StockStatus = "In Stock";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 