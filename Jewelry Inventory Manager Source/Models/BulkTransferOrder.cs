using System;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public enum TransferStatus
    {
        Created,        // Transfer order created, items being prepared
        InTransit,      // Items shipped/transported to destination
        Delivered,      // Items arrived at destination
        Completed,      // Stock updated at destination, transfer complete
        Cancelled       // Transfer cancelled
    }

    public class BulkTransferOrder : INotifyPropertyChanged
    {
        private int _id;
        private string _transferNumber;
        private int _fromLocationId;
        private int _toLocationId;
        private string _fromLocationName;
        private string _toLocationName;
        private TransferStatus _status;
        private DateTime _createdDate;
        private DateTime? _shippedDate;
        private DateTime? _deliveredDate;
        private DateTime? _completedDate;
        private string _notes;
        private string _trackingNumber;
        private string _createdBy;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string TransferNumber
        {
            get => _transferNumber;
            set
            {
                _transferNumber = value;
                OnPropertyChanged(nameof(TransferNumber));
            }
        }

        public int FromLocationId
        {
            get => _fromLocationId;
            set
            {
                _fromLocationId = value;
                OnPropertyChanged(nameof(FromLocationId));
            }
        }

        public int ToLocationId
        {
            get => _toLocationId;
            set
            {
                _toLocationId = value;
                OnPropertyChanged(nameof(ToLocationId));
            }
        }

        public string FromLocationName
        {
            get => _fromLocationName;
            set
            {
                _fromLocationName = value;
                OnPropertyChanged(nameof(FromLocationName));
            }
        }

        public string ToLocationName
        {
            get => _toLocationName;
            set
            {
                _toLocationName = value;
                OnPropertyChanged(nameof(ToLocationName));
            }
        }

        public TransferStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
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

        public DateTime? ShippedDate
        {
            get => _shippedDate;
            set
            {
                _shippedDate = value;
                OnPropertyChanged(nameof(ShippedDate));
            }
        }

        public DateTime? DeliveredDate
        {
            get => _deliveredDate;
            set
            {
                _deliveredDate = value;
                OnPropertyChanged(nameof(DeliveredDate));
            }
        }

        public DateTime? CompletedDate
        {
            get => _completedDate;
            set
            {
                _completedDate = value;
                OnPropertyChanged(nameof(CompletedDate));
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

        public string TrackingNumber
        {
            get => _trackingNumber;
            set
            {
                _trackingNumber = value;
                OnPropertyChanged(nameof(TrackingNumber));
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                _createdBy = value;
                OnPropertyChanged(nameof(CreatedBy));
            }
        }

        public int ItemCount { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 