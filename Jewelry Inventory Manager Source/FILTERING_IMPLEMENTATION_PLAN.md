# Filtering System Implementation Plan

## ✅ **Completed Windows**

### 1. **SetMinimumStockWindow** ✅
- **Status**: Fully implemented with FilteredComboBox
- **Features**: Search, category filter, location filter, in-stock only
- **Layout**: Vertical layout with proper spacing

### 2. **AddInventoryTransactionWindow** ✅
- **Status**: Fully implemented with FilteredComboBox
- **Features**: Search, category filter, location filter, in-stock only
- **Layout**: Integrated into existing form

## 🔄 **Windows to Update**

### 3. **InventoryTransferWindow** ✅
- **Status**: Fully implemented with FilteredComboBox
- **Features**: Search, category filter, location filter, in-stock only
- **Layout**: Integrated into existing form
- **Special Features**: Available stock calculation based on selected item and location

### 4. **AddOrderWindow** ✅
- **Status**: Fully implemented with FilteredComboBox
- **Features**: Search, category filter, location filter, in-stock only
- **Layout**: Integrated into existing form
- **Special Features**: Product selection with automatic unit price display

### 5. **ComponentTransformationWindow** ✅
- **Status**: Fully implemented with FilteredComboBoxes
- **Features**: Search, category filter, location filter, in-stock only
- **Layout**: Integrated into existing form
- **Special Features**: Multiple component selection points (source, add, result, combine result)

### 6. **AddProductWindow** (Medium Priority)
- **Current**: Uses ComboBox for component selection
- **Location**: `Views/AddProductWindow.xaml`
- **Changes Needed**:
  - Replace component ComboBox with FilteredComboBox
  - Set item type to Components only (false)

### 7. **EditProductWindow** (Medium Priority)
- **Current**: Uses ComboBox for component selection
- **Location**: `Moonglow_DB/Views/EditProductWindow.xaml`
- **Changes Needed**:
  - Replace component ComboBox with FilteredComboBox
  - Set item type to Components only (false)

## 📋 **Implementation Steps for Each Window**

### Step 1: Update XAML
```xml
<!-- Add namespace reference -->
xmlns:Controls="clr-namespace:Moonglow_DB.Views.Controls"

<!-- Replace ComboBox with FilteredComboBox -->
<Controls:FilteredComboBox x:Name="filteredItemComboBox" 
                          SelectionChanged="FilteredItemComboBox_SelectionChanged"/>
```

### Step 2: Update Code-Behind
```csharp
// Add using statement
using Moonglow_DB.Views.Controls;

// Add initialization method
private void InitializeFilteredComboBox()
{
    try
    {
        var allProducts = _databaseContext.GetAllProducts().Where(p => p.IsActive).ToList();
        var allComponents = _databaseContext.GetAllComponents().Where(c => c.IsActive).ToList();
        
        var filterService = new ItemFilterService(_databaseContext);
        filteredItemComboBox.Initialize(filterService, allProducts, allComponents);
        
        // Set appropriate item type
        filteredItemComboBox.SetItemType(true); // true = Products, false = Components
    }
    catch (Exception ex)
    {
        ErrorDialog.ShowError($"Error initializing filtered combo box: {ex.Message}", "Error");
    }
}

// Add selection changed handler
private void FilteredItemComboBox_SelectionChanged(object sender, object selectedItem)
{
    if (selectedItem is ComboBoxDisplayItem displayItem)
    {
        // Handle the selected item based on window requirements
    }
}
```

### Step 3: Update Item Type Selection (if applicable)
```csharp
private void cmbItemType_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (cmbItemType.SelectedItem is ComboBoxItem selectedItem)
    {
        bool isProduct = selectedItem.Content.ToString() == "Product";
        filteredItemComboBox.SetItemType(isProduct);
    }
}
```

## 🎯 **Priority Order**

1. **InventoryTransferWindow** - High impact, frequently used
2. **AddOrderWindow** - High impact, order management
3. **ComponentTransformationWindow** - Medium impact, complex component operations
4. **AddProductWindow** - Medium impact, product creation
5. **EditProductWindow** - Medium impact, product editing

## 🚀 **Benefits After Implementation**

- **Consistent UX**: All windows will have the same filtering experience
- **Better Performance**: Filtered dropdowns reduce scrolling and improve selection
- **Enhanced Search**: Text search across all product/component selections
- **Category Filtering**: Filter by category in all relevant windows
- **Stock Filtering**: Option to show only in-stock items
- **Reusable Code**: Single FilteredComboBox component used everywhere

## 📝 **Testing Checklist**

For each implemented window:
- [ ] Search functionality works
- [ ] Category filtering works
- [ ] Item type switching works (Product/Component)
- [ ] Selection events fire correctly
- [ ] Validation still works
- [ ] No layout issues
- [ ] Performance is acceptable with large datasets 