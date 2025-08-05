using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class ProductComponentSelection : INotifyPropertyChanged
    {
        private Component _component;
        private int _quantity;
        private decimal _totalCost;

        public Component Component
        {
            get => _component;
            set
            {
                _component = value;
                CalculateTotalCost();
                OnPropertyChanged(nameof(Component));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                CalculateTotalCost();
                OnPropertyChanged(nameof(Quantity));
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

        private void CalculateTotalCost()
        {
            if (Component != null)
            {
                TotalCost = Component.Cost * Quantity;
            }
            else
            {
                TotalCost = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 