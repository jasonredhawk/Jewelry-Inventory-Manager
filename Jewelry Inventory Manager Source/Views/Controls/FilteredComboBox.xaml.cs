using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Moonglow_DB.Models;

namespace Moonglow_DB.Views.Controls
{
    public partial class FilteredComboBox : UserControl
    {
        private ItemFilterService _filterService;
        private List<Product> _allProducts;
        private List<Component> _allComponents;
        private bool _isProductMode = true;

        public event EventHandler<object> SelectionChanged;

        public FilteredComboBox()
        {
            InitializeComponent();
        }

        public void Initialize(ItemFilterService filterService, List<Product> allProducts, List<Component> allComponents)
        {
            _filterService = filterService;
            _allProducts = allProducts;
            _allComponents = allComponents;
            
            itemFilterControl.Initialize(filterService);
            itemFilterControl.FilterChanged += ItemFilterControl_FilterChanged;
            
            SetItemType(true); // Default to products
        }

        public void SetItemType(bool isProduct)
        {
            _isProductMode = isProduct;
            lblItemType.Text = isProduct ? "Product:" : "Component:";
            
            // Only refresh if initialized
            if (_filterService != null && itemFilterControl != null)
            {
                RefreshItems();
            }
        }

        private void ItemFilterControl_FilterChanged(object sender, ItemFilterCriteria criteria)
        {
            // Only refresh if initialized
            if (_filterService != null && itemFilterControl != null)
            {
                RefreshItems();
            }
        }

        private void RefreshItems()
        {
            try
            {
                // Check if all required objects are initialized
                if (itemFilterControl == null || _filterService == null)
                {
                    return; // Not initialized yet
                }

                var criteria = itemFilterControl.GetCurrentCriteria();
                
                if (_isProductMode)
                {
                    if (_allProducts == null)
                    {
                        cmbItem.ItemsSource = new List<ComboBoxDisplayItem>();
                        return;
                    }

                    var filteredProducts = _filterService.FilterProducts(criteria, _allProducts);
                    var displayItems = filteredProducts.Select(p => new ComboBoxDisplayItem
                    {
                        Id = p.Id,
                        DisplayText = $"{p.Name} (SKU: {p.SKU})",
                        Item = p
                    }).ToList();
                    
                    cmbItem.ItemsSource = displayItems;
                }
                else
                {
                    if (_allComponents == null)
                    {
                        cmbItem.ItemsSource = new List<ComboBoxDisplayItem>();
                        return;
                    }

                    var filteredComponents = _filterService.FilterComponents(criteria, _allComponents);
                    var displayItems = filteredComponents.Select(c => new ComboBoxDisplayItem
                    {
                        Id = c.Id,
                        DisplayText = $"{c.Name} (SKU: {c.SKU})",
                        Item = c
                    }).ToList();
                    
                    cmbItem.ItemsSource = displayItems;
                }
            }
            catch (Exception ex)
            {
                // Log the error and set empty items source
                System.Diagnostics.Debug.WriteLine($"Error in RefreshItems: {ex.Message}");
                cmbItem.ItemsSource = new List<ComboBoxDisplayItem>();
            }
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, cmbItem.SelectedItem);
        }

        public object GetSelectedItem()
        {
            if (cmbItem.SelectedItem is ComboBoxDisplayItem displayItem)
            {
                return displayItem.Item;
            }
            return null;
        }

        public int? GetSelectedItemId()
        {
            if (cmbItem.SelectedItem is ComboBoxDisplayItem displayItem)
            {
                return displayItem.Id;
            }
            return null;
        }

        public void SetSelectedItem(int itemId)
        {
            foreach (ComboBoxDisplayItem item in cmbItem.Items)
            {
                if (item.Id == itemId)
                {
                    cmbItem.SelectedItem = item;
                    break;
                }
            }
        }

        public void ClearSelection()
        {
            cmbItem.SelectedIndex = -1;
        }

        public void SetFilterCriteria(ItemFilterCriteria criteria)
        {
            if (criteria.SearchText != null)
                itemFilterControl.SetSearchText(criteria.SearchText);
            
            if (criteria.CategoryId.HasValue)
                itemFilterControl.SetCategoryFilter(criteria.CategoryId);
            
            if (criteria.LocationId.HasValue)
                itemFilterControl.SetLocationFilter(criteria.LocationId);
            
            itemFilterControl.SetInStockOnly(criteria.InStockOnly);
        }

        public ItemFilterCriteria GetFilterCriteria()
        {
            return itemFilterControl.GetCurrentCriteria();
        }

        public void ResetFilters()
        {
            itemFilterControl.ResetFilters();
        }
    }

    public class ComboBoxDisplayItem
    {
        public int Id { get; set; }
        public string DisplayText { get; set; }
        public object Item { get; set; }
    }
} 