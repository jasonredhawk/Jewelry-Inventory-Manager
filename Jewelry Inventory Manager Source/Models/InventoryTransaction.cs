using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public enum TransactionType
    {
        Sale,
        Purchase,
        Transfer,
        TransferOut,
        TransferIn,
        Adjustment,
        Return,
        BreakDown,
        Damage,
        Expiry
    }

    public class InventoryTransaction : INotifyPropertyChanged
    {
        private int _id;
        private DateTime _transactionDate;
        private TransactionType _transactionType;
        private int? _locationId;
        private int _itemId;
        private string _itemType;
        private int _quantity;
        private string _notes;
        private int? _orderId;
        private string _locationName;
        private string _itemName;
        private string _transactionTypeDisplay;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public DateTime TransactionDate
        {
            get => _transactionDate;
            set
            {
                _transactionDate = value;
                OnPropertyChanged(nameof(TransactionDate));
            }
        }

        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                TransactionTypeDisplay = GetTransactionTypeDisplay(value);
                OnPropertyChanged(nameof(TransactionType));
            }
        }

        public int? LocationId
        {
            get => _locationId;
            set
            {
                _locationId = value;
                OnPropertyChanged(nameof(LocationId));
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

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
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

        public int? OrderId
        {
            get => _orderId;
            set
            {
                _orderId = value;
                OnPropertyChanged(nameof(OrderId));
            }
        }

        // Display properties for UI
        public string LocationName
        {
            get => _locationName;
            set
            {
                _locationName = value;
                OnPropertyChanged(nameof(LocationName));
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

        public string TransactionTypeDisplay
        {
            get => _transactionTypeDisplay;
            set
            {
                _transactionTypeDisplay = value;
                OnPropertyChanged(nameof(TransactionTypeDisplay));
            }
        }

        private string GetTransactionTypeDisplay(TransactionType type)
        {
            return type switch
            {
                TransactionType.Purchase => "Purchase",
                TransactionType.Sale => "Sale",
                TransactionType.Transfer => "Transfer",
                TransactionType.Adjustment => "Adjustment",
                TransactionType.Return => "Return",
                TransactionType.BreakDown => "Break Down",
                TransactionType.Damage => "Damage",
                TransactionType.Expiry => "Expiry",
                _ => "Unknown"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 