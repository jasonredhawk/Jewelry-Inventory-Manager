using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views.Controls
{
    public partial class ItemFilterControl : UserControl
    {
        private ItemFilterService _filterService;
        private ItemFilterCriteria _currentCriteria;
        private List<Category> _allCategories;
        private List<Location> _allLocations;

        public event EventHandler<ItemFilterCriteria> FilterChanged;

        public ItemFilterControl()
        {
            InitializeComponent();
            _currentCriteria = new ItemFilterCriteria();
        }

        public void Initialize(ItemFilterService filterService)
        {
            _filterService = filterService;
            LoadCategories();
            LoadLocations();
            ResetFilters();
        }

        private void LoadCategories()
        {
            try
            {
                _allCategories = _filterService.GetActiveCategories();
                cmbCategory.Items.Clear();
                cmbCategory.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = (int?)null, IsSelected = true });
                
                foreach (var category in _allCategories)
                {
                    cmbCategory.Items.Add(new ComboBoxItem 
                    { 
                        Content = category.Name, 
                        Tag = category.Id 
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLocations()
        {
            try
            {
                _allLocations = _filterService.GetActiveLocations();
                cmbLocation.Items.Clear();
                cmbLocation.Items.Add(new ComboBoxItem { Content = "All Locations", Tag = (int?)null, IsSelected = true });
                
                foreach (var location in _allLocations)
                {
                    cmbLocation.Items.Add(new ComboBoxItem 
                    { 
                        Content = location.Name, 
                        Tag = location.Id 
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading locations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentCriteria.SearchText = txtSearch.Text;
            OnFilterChanged();
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategory.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentCriteria.CategoryId = selectedItem.Tag as int?;
                OnFilterChanged();
            }
        }

        private void cmbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLocation.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentCriteria.LocationId = selectedItem.Tag as int?;
                OnFilterChanged();
            }
        }

        private void chkInStockOnly_Changed(object sender, RoutedEventArgs e)
        {
            _currentCriteria.InStockOnly = chkInStockOnly.IsChecked ?? false;
            OnFilterChanged();
        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ResetFilters();
        }

        public void ResetFilters()
        {
            txtSearch.Text = "";
            cmbCategory.SelectedIndex = 0;
            cmbLocation.SelectedIndex = 0;
            chkInStockOnly.IsChecked = false;
            
            _currentCriteria = new ItemFilterCriteria();
            OnFilterChanged();
        }

        public ItemFilterCriteria GetCurrentCriteria()
        {
            return _currentCriteria;
        }

        private void OnFilterChanged()
        {
            FilterChanged?.Invoke(this, _currentCriteria);
        }

        public void SetSearchText(string searchText)
        {
            txtSearch.Text = searchText;
        }

        public void SetCategoryFilter(int? categoryId)
        {
            if (categoryId.HasValue)
            {
                foreach (ComboBoxItem item in cmbCategory.Items)
                {
                    if (item.Tag is int id && id == categoryId.Value)
                    {
                        cmbCategory.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                cmbCategory.SelectedIndex = 0;
            }
        }

        public void SetLocationFilter(int? locationId)
        {
            if (locationId.HasValue)
            {
                foreach (ComboBoxItem item in cmbLocation.Items)
                {
                    if (item.Tag is int id && id == locationId.Value)
                    {
                        cmbLocation.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                cmbLocation.SelectedIndex = 0;
            }
        }

        public void SetInStockOnly(bool inStockOnly)
        {
            chkInStockOnly.IsChecked = inStockOnly;
        }
    }
} 