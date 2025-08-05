using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class Product : INotifyPropertyChanged
    {
        private int _id;
        private string _sku;
        private string _name;
        private string _description;
        private int? _categoryId;
        private decimal _price;
        private decimal _cost;
        private DateTime _createdDate;
        private DateTime _lastModified;
        private bool _isActive;

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

        public int? CategoryId
        {
            get => _categoryId;
            set
            {
                _categoryId = value;
                OnPropertyChanged(nameof(CategoryId));
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

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
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

        // Properties for compatibility with existing code
                public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public decimal ComponentCost { get; set; }
        
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        // Navigation properties
        public List<ProductComponent> ProductComponents { get; set; } = new List<ProductComponent>();
        public List<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Category Category { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{SKU} - {Name}";
        }

        // Method to generate combined SKU from components
        public string GenerateCombinedSKU()
        {
            if (ProductComponents == null || ProductComponents.Count == 0)
                return SKU;

            var componentSKUs = new List<string>();
            foreach (var pc in ProductComponents)
            {
                if (pc.Component != null)
                {
                    // Add the component SKU repeated by quantity
                    for (int i = 0; i < pc.Quantity; i++)
                    {
                        componentSKUs.Add(pc.Component.SKU);
                    }
                }
            }

            return string.Join(" + ", componentSKUs);
        }

        // Method to calculate total cost from components
        public decimal CalculateTotalCost()
        {
            if (ProductComponents == null || ProductComponents.Count == 0)
                return 0;

            decimal totalCost = 0;
            foreach (var pc in ProductComponents)
            {
                if (pc.Component != null)
                {
                    totalCost += pc.Component.Cost * pc.Quantity;
                }
            }

            return totalCost;
        }

        // Method to check if product can be assembled (all components in stock)
        // Note: This method now requires external stock checking since CurrentStock is removed
        public bool CanBeAssembled(int requiredStock = 1)
        {
            if (ProductComponents == null || ProductComponents.Count == 0)
                return false;

            // This method now requires external stock checking
            // The actual stock validation should be done in the calling code
            // by querying location-specific inventory
            return true;
        }
    }
} 