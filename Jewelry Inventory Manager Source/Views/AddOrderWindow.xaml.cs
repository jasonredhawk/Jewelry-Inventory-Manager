using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Models;
using Moonglow_DB.Data;

namespace Moonglow_DB.Views
{
    public partial class AddOrderWindow : Window
    {
        private List<Product> _allProducts;
        private List<Customer> _allCustomers;
        private List<OrderItem> _selectedItems;
        private readonly DatabaseContext _databaseContext;

        public AddOrderWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            _selectedItems = new List<OrderItem>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                LoadProducts();
                LoadCustomers();
                LoadEmployees();
                LoadOrderTypes();
                LoadOrderStatuses();
                GenerateOrderNumber();
                UpdateOrderItemsList();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadProducts()
        {
            try
            {
                _allProducts = _databaseContext.GetAllProducts();
                cmbProduct.Items.Clear();
                cmbProduct.Items.Add(new ComboBoxItem { Content = "Select Product", Tag = (int?)null });
                
                foreach (var product in _allProducts.Where(p => p.IsActive))
                {
                    cmbProduct.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{product.Name} (${product.Price:F2})", 
                        Tag = product.Id 
                    });
                }
                
                cmbProduct.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading products: {ex.Message}", "Database Error");
            }
        }

        private void LoadCustomers()
        {
            try
            {
                var customers = _databaseContext.GetAllCustomers();
                cmbCustomer.Items.Clear();
                cmbCustomer.Items.Add(new ComboBoxItem { Content = "Select Customer", Tag = (int?)null });
                
                foreach (var customer in customers.Where(c => c.IsActive))
                {
                    cmbCustomer.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{customer.FirstName} {customer.LastName}", 
                        Tag = customer.Id 
                    });
                }
                
                cmbCustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading customers: {ex.Message}", "Database Error");
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = _databaseContext.GetAllEmployees();
                cmbEmployee.Items.Clear();
                cmbEmployee.Items.Add(new ComboBoxItem { Content = "Select Employee", Tag = (int?)null });
                
                foreach (var employee in employees.Where(e => e.IsActive))
                {
                    cmbEmployee.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{employee.FirstName} {employee.LastName}", 
                        Tag = employee.Id 
                    });
                }
                
                cmbEmployee.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading employees: {ex.Message}", "Database Error");
            }
        }

        private void LoadOrderTypes()
        {
            try
            {
                cmbOrderType.Items.Clear();
                cmbOrderType.Items.Add("Sale");
                cmbOrderType.Items.Add("Transfer");
                cmbOrderType.Items.Add("Purchase");
                cmbOrderType.Items.Add("Return");
                cmbOrderType.SelectedIndex = 0; // Default to "Sale"
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading order types: {ex.Message}", "Error");
            }
        }

        private void LoadOrderStatuses()
        {
            try
            {
                cmbOrderStatus.Items.Clear();
                cmbOrderStatus.Items.Add("Pending");
                cmbOrderStatus.Items.Add("Confirmed");
                cmbOrderStatus.Items.Add("Shipped");
                cmbOrderStatus.Items.Add("Delivered");
                cmbOrderStatus.Items.Add("Cancelled");
                cmbOrderStatus.SelectedIndex = 0; // Default to "Pending"
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading order statuses: {ex.Message}", "Error");
            }
        }

        private void GenerateOrderNumber()
        {
            try
            {
                // Generate a unique order number
                var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
                txtOrderNumber.Text = orderNumber;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error generating order number: {ex.Message}", "Error");
            }
        }

        private void UpdateOrderItemsList()
        {
            dgOrderItems.ItemsSource = null;
            dgOrderItems.ItemsSource = _selectedItems;
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProduct.SelectedItem == null)
            {
                ErrorDialog.ShowWarning("Please select a product.", "Validation Error");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                ErrorDialog.ShowWarning("Please enter a valid quantity.", "Invalid Quantity");
                return;
            }

            if (cmbProduct.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int productId)
            {
                // Find the product in the loaded products list
                var product = _allProducts.FirstOrDefault(p => p.Id == productId);
                if (product != null)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * quantity
                    };

                    _selectedItems.Add(orderItem);
                    UpdateOrderItemsList();
                    ClearProductForm();
                }
                else
                {
                    ErrorDialog.ShowWarning("Selected product not found.", "Validation Error");
                }
            }
            else
            {
                ErrorDialog.ShowWarning("Please select a valid product.", "Validation Error");
            }
        }

        private void btnRemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrderItems.SelectedItem is OrderItem selectedItem)
            {
                _selectedItems.Remove(selectedItem);
                UpdateOrderItemsList();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                CreateOrder();
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error creating order: {ex.Message}", "Database Error");
            }
        }

        private bool ValidateInput()
        {
            if (_selectedItems.Count == 0)
            {
                ErrorDialog.ShowWarning("Please add at least one product to the order.", "Validation Error");
                return false;
            }

            return true;
        }

        private void CreateOrder()
        {
            try
            {
                using var connection = _databaseContext.GetConnection();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Insert the order
                    var orderSql = @"
                        INSERT INTO Orders (OrderNumber, CustomerId, EmployeeId, OrderType, Status, CreatedDate, LastModified) 
                        VALUES (@orderNumber, @customerId, @employeeId, @orderType, @status, @createdDate, @lastModified);
                        SELECT LAST_INSERT_ID();";

                    int orderId;
                    using (var command = _databaseContext.CreateCommand(orderSql))
                    {
                        command.Transaction = transaction;
                        command.Parameters.AddWithValue("@orderNumber", GetOrderNumber());
                        command.Parameters.AddWithValue("@customerId", GetSelectedCustomerId() ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@employeeId", GetSelectedEmployeeId() ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@orderType", GetSelectedOrderType());
                        command.Parameters.AddWithValue("@status", GetSelectedOrderStatus());
                        command.Parameters.AddWithValue("@createdDate", DateTime.Now);
                        command.Parameters.AddWithValue("@lastModified", DateTime.Now);
                        
                        orderId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Insert order items
                    var itemSql = @"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, TotalPrice) 
                        VALUES (@orderId, @productId, @quantity, @unitPrice, @totalPrice)";

                    foreach (var item in _selectedItems)
                    {
                        using var command = _databaseContext.CreateCommand(itemSql);
                        command.Transaction = transaction;
                        command.Parameters.AddWithValue("@orderId", orderId);
                        command.Parameters.AddWithValue("@productId", item.ProductId);
                        command.Parameters.AddWithValue("@quantity", item.Quantity);
                        command.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                        command.Parameters.AddWithValue("@totalPrice", item.TotalPrice);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    ErrorDialog.ShowSuccess("Order created successfully!", "Success");
                    DialogResult = true;
                    Close();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error creating order: {ex.Message}", "Database Error");
            }
        }

        private string GetOrderNumber()
        {
            if (!string.IsNullOrWhiteSpace(txtOrderNumber.Text))
                return txtOrderNumber.Text.Trim();
            
            // Generate a default order number if none provided
            return $"ORD-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
        }

        private int? GetSelectedCustomerId()
        {
            if (cmbCustomer.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int customerId)
                return customerId;
            return null;
        }

        private int? GetSelectedEmployeeId()
        {
            if (cmbEmployee.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int employeeId)
                return employeeId;
            return null;
        }

        private string GetSelectedOrderType()
        {
            if (cmbOrderType.SelectedItem is string orderType)
                return orderType;
            return "Sale";
        }

        private string GetSelectedOrderStatus()
        {
            if (cmbOrderStatus.SelectedItem is string orderStatus)
                return orderStatus;
            return "Pending";
        }

        private void ClearProductForm()
        {
            cmbProduct.SelectedIndex = -1;
            txtQuantity.Text = string.Empty;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtQuantity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            var regex = new System.Text.RegularExpressions.Regex(@"^\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void cmbCustomer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void cmbEmployee_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void cmbOrderType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void cmbOrderStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }

        private void cmbProduct_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Temporarily disabled
        }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 