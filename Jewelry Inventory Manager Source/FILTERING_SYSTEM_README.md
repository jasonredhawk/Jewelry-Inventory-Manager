# Reusable Product and Component Filtering System

This document explains how to use the new reusable filtering system for products and components in the Jewelry Inventory Manager.

## Overview

The filtering system consists of three main components:

1. **ItemFilterService** - Handles the filtering logic
2. **ItemFilterControl** - A reusable UserControl for filtering UI
3. **FilteredComboBox** - A complete dropdown with integrated filtering

## Components

### ItemFilterService

Located in `Models/ItemFilterService.cs`, this service handles all filtering logic for products and components.

**Features:**
- Text search (name, SKU, description)
- Category filtering
- Active/Inactive filtering
- Stock filtering by location
- Combined filtering criteria

**Usage:**
```csharp
var filterService = new ItemFilterService(databaseContext);
var criteria = new ItemFilterCriteria
{
    SearchText = "gold",
    CategoryId = 5,
    ActiveOnly = true,
    LocationId = 1,
    InStockOnly = false
};

var filteredProducts = filterService.FilterProducts(criteria);
var filteredComponents = filterService.FilterComponents(criteria);
```

### ItemFilterControl

Located in `Views/Controls/ItemFilterControl.xaml`, this is a reusable UserControl that provides filtering UI.

**Features:**
- Search text box
- Category dropdown
- Location dropdown (for stock filtering)
- "In Stock Only" checkbox
- Clear filters button
- Real-time filtering

**Usage:**
```xml
<Controls:ItemFilterControl x:Name="itemFilterControl" 
                           FilterChanged="ItemFilterControl_FilterChanged"/>
```

```csharp
// Initialize
itemFilterControl.Initialize(filterService);

// Subscribe to filter changes
itemFilterControl.FilterChanged += (sender, criteria) => {
    // Handle filter changes
    RefreshItems(criteria);
};

// Set specific filters
itemFilterControl.SetSearchText("gold");
itemFilterControl.SetCategoryFilter(5);
itemFilterControl.SetLocationFilter(1);
itemFilterControl.SetInStockOnly(true);
```

### FilteredComboBox

Located in `Views/Controls/FilteredComboBox.xaml`, this is a complete dropdown with integrated filtering.

**Features:**
- Integrated filtering UI
- Product/Component mode switching
- Automatic item refresh on filter changes
- Easy selection handling

**Usage:**
```xml
<Controls:FilteredComboBox x:Name="filteredItemComboBox" 
                           SelectionChanged="FilteredItemComboBox_SelectionChanged"/>
```

```csharp
// Initialize
filteredItemComboBox.Initialize(filterService, allProducts, allComponents);

// Set item type (Product or Component)
filteredItemComboBox.SetItemType(true); // true = Product, false = Component

// Subscribe to selection changes
filteredItemComboBox.SelectionChanged += (sender, selectedItem) => {
    if (selectedItem is ComboBoxDisplayItem displayItem)
    {
        var item = displayItem.Item;
        var itemId = displayItem.Id;
        // Handle selection
    }
};

// Get selected item
var selectedItem = filteredItemComboBox.GetSelectedItem();
var selectedItemId = filteredItemComboBox.GetSelectedItemId();

// Set selected item
filteredItemComboBox.SetSelectedItem(itemId);

// Clear selection
filteredItemComboBox.ClearSelection();
```

## Implementation Example

Here's how to implement the filtering system in a new window:

### 1. Add the namespace to XAML
```xml
<Window xmlns:Controls="clr-namespace:Moonglow_DB.Views.Controls">
```

### 2. Add the FilteredComboBox to your XAML
```xml
<Controls:FilteredComboBox x:Name="filteredItemComboBox" 
                           SelectionChanged="FilteredItemComboBox_SelectionChanged"/>
```

### 3. Initialize in code-behind
```csharp
public partial class MyWindow : Window
{
    private readonly DatabaseContext _databaseContext;
    private ItemFilterService _filterService;
    private List<Product> _allProducts;
    private List<Component> _allComponents;

    public MyWindow(DatabaseContext databaseContext)
    {
        InitializeComponent();
        _databaseContext = databaseContext;
        InitializeFiltering();
    }

    private void InitializeFiltering()
    {
        // Load data
        _allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
        _allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
        
        // Create filter service
        _filterService = new ItemFilterService(_databaseContext);
        
        // Initialize filtered combo box
        filteredItemComboBox.Initialize(_filterService, _allProducts, _allComponents);
        filteredItemComboBox.SetItemType(false); // Start with components
    }

    private void FilteredItemComboBox_SelectionChanged(object sender, object selectedItem)
    {
        if (selectedItem is ComboBoxDisplayItem displayItem)
        {
            var itemId = displayItem.Id;
            var item = displayItem.Item;
            // Handle the selection
        }
    }
}
```

## Benefits

1. **Reusable** - Use the same filtering system across all windows
2. **Consistent UI** - All filtering interfaces look and behave the same
3. **Performance** - Efficient filtering for large datasets
4. **Flexible** - Easy to customize filtering criteria
5. **User-friendly** - Real-time filtering with clear visual feedback

## Migration Guide

To migrate an existing window to use the new filtering system:

1. Replace the old ComboBox with FilteredComboBox
2. Remove manual item loading code
3. Update selection handling to use the new event
4. Initialize the FilteredComboBox in the constructor

## Example Migration

**Before:**
```xml
<ComboBox x:Name="cmbItem" SelectionChanged="cmbItem_SelectionChanged"/>
```

```csharp
private void LoadItems()
{
    cmbItem.Items.Clear();
    foreach (var item in allItems)
    {
        cmbItem.Items.Add(new ComboBoxItem { Content = item.Name, Tag = item.Id });
    }
}
```

**After:**
```xml
<Controls:FilteredComboBox x:Name="filteredItemComboBox" 
                           SelectionChanged="FilteredItemComboBox_SelectionChanged"/>
```

```csharp
private void InitializeFiltering()
{
    var filterService = new ItemFilterService(_databaseContext);
    filteredItemComboBox.Initialize(filterService, allProducts, allComponents);
}
```

The new system automatically handles filtering, loading, and selection management. 