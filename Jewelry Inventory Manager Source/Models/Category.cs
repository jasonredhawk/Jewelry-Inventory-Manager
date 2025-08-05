using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Moonglow_DB.Models
{
    public class Category : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _description;
        private int? _parentId;
        private int _sortOrder;
        private bool _isActive;
        private DateTime _createdDate;
        private DateTime _lastModified;
        private Category _parent;
        private List<Category> _children;

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

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public int? ParentId
        {
            get => _parentId;
            set
            {
                _parentId = value;
                OnPropertyChanged(nameof(ParentId));
            }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                _sortOrder = value;
                OnPropertyChanged(nameof(SortOrder));
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

        // Navigation properties
        public Category Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                OnPropertyChanged(nameof(Parent));
            }
        }

        public List<Category> Children
        {
            get => _children ?? (_children = new List<Category>());
            set
            {
                _children = value;
                OnPropertyChanged(nameof(Children));
            }
        }

        // Display properties
        public string FullName
        {
            get
            {
                if (Parent != null)
                    return $"{Parent.Name} → {Name}";
                return Name;
            }
        }

        public string Level
        {
            get
            {
                var level = 0;
                var current = this;
                while (current.Parent != null)
                {
                    level++;
                    current = current.Parent;
                }
                return new string(' ', level * 2);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return FullName;
        }
    }
} 