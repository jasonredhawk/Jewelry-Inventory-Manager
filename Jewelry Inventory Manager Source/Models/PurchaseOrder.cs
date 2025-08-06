using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class PurchaseOrder : INotifyPropertyChanged
    {
        private int _id;
        private string _poNumber;
        private DateTime _poDate;
        private decimal _totalValue;
        private POStatus _status;
        private string _supplierName;
        private string _supplierContact;
        private DateTime? _expectedDeliveryDate;
        private DateTime? _actualDeliveryDate;
        private string _notes;
        private DateTime _createdDate;
        private DateTime _updatedDate;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string PONumber
        {
            get => _poNumber;
            set
            {
                _poNumber = value;
                OnPropertyChanged(nameof(PONumber));
            }
        }

        public DateTime PODate
        {
            get => _poDate;
            set
            {
                _poDate = value;
                OnPropertyChanged(nameof(PODate));
            }
        }

        public decimal TotalValue
        {
            get => _totalValue;
            set
            {
                _totalValue = value;
                OnPropertyChanged(nameof(TotalValue));
            }
        }

        public POStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string SupplierName
        {
            get => _supplierName;
            set
            {
                _supplierName = value;
                OnPropertyChanged(nameof(SupplierName));
            }
        }

        public string SupplierContact
        {
            get => _supplierContact;
            set
            {
                _supplierContact = value;
                OnPropertyChanged(nameof(SupplierContact));
            }
        }

        public DateTime? ExpectedDeliveryDate
        {
            get => _expectedDeliveryDate;
            set
            {
                _expectedDeliveryDate = value;
                OnPropertyChanged(nameof(ExpectedDeliveryDate));
            }
        }

        public DateTime? ActualDeliveryDate
        {
            get => _actualDeliveryDate;
            set
            {
                _actualDeliveryDate = value;
                OnPropertyChanged(nameof(ActualDeliveryDate));
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

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public DateTime UpdatedDate
        {
            get => _updatedDate;
            set
            {
                _updatedDate = value;
                OnPropertyChanged(nameof(UpdatedDate));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum POStatus
    {
        Created,
        Ordered,
        Shipped,
        Received,
        Completed,
        Cancelled
    }
} 