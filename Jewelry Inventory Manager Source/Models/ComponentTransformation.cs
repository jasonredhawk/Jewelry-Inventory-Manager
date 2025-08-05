using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public enum TransformationType
    {
        BreakDown,  // Break one component into multiple smaller components
        Combine     // Combine multiple components into one larger component
    }

    public class ComponentTransformation : INotifyPropertyChanged
    {
        private int _id;
        private DateTime _transformationDate;
        private TransformationType _transformationType;
        private int _locationId;
        private string _notes;
        private List<TransformationItem> _sourceItems;
        private List<TransformationItem> _resultItems;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public DateTime TransformationDate
        {
            get => _transformationDate;
            set
            {
                _transformationDate = value;
                OnPropertyChanged(nameof(TransformationDate));
            }
        }

        public TransformationType TransformationType
        {
            get => _transformationType;
            set
            {
                _transformationType = value;
                OnPropertyChanged(nameof(TransformationType));
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

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public List<TransformationItem> SourceItems
        {
            get => _sourceItems ?? (_sourceItems = new List<TransformationItem>());
            set
            {
                _sourceItems = value;
                OnPropertyChanged(nameof(SourceItems));
            }
        }

        public List<TransformationItem> ResultItems
        {
            get => _resultItems ?? (_resultItems = new List<TransformationItem>());
            set
            {
                _resultItems = value;
                OnPropertyChanged(nameof(ResultItems));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TransformationItem : INotifyPropertyChanged
    {
        private int _id;
        private int _componentId;
        private string _componentName;
        private string _componentSKU;
        private int _quantity;
        private decimal _unitCost;
        private decimal _totalCost;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public int ComponentId
        {
            get => _componentId;
            set
            {
                _componentId = value;
                OnPropertyChanged(nameof(ComponentId));
            }
        }

        public string ComponentName
        {
            get => _componentName;
            set
            {
                _componentName = value;
                OnPropertyChanged(nameof(ComponentName));
            }
        }

        public string ComponentSKU
        {
            get => _componentSKU;
            set
            {
                _componentSKU = value;
                OnPropertyChanged(nameof(ComponentSKU));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                UpdateTotalCost();
            }
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                _unitCost = value;
                OnPropertyChanged(nameof(UnitCost));
                UpdateTotalCost();
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

        private void UpdateTotalCost()
        {
            TotalCost = Quantity * UnitCost;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 