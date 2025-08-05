using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class LocationInventoryItem : INotifyPropertyChanged
    {
        private int _id;
        private string _sku;
        private string _name;
        private string _description;
        private string _itemType;
        private int _locationId;
        private string _locationName;
        private int _currentStock;
        private int _minimumStock;
        private decimal _price;
        private decimal _cost;
        private bool _isActive;
        private DateTime _lastModified;
        private string _stockStatus;
        private int _totalStockAcrossLocations;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
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

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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

        public string ItemType
        {
            get => _itemType;
            set
            {
                _itemType = value;
                OnPropertyChanged(nameof(ItemType));
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

        public int TotalStockAcrossLocations
        {
            get => _totalStockAcrossLocations;
            set
            {
                _totalStockAcrossLocations = value;
                OnPropertyChanged(nameof(TotalStockAcrossLocations));
            }
        }

        private bool _isSelected;
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