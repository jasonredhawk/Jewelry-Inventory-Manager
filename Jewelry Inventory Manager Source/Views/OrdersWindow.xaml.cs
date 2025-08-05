using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Moonglow_DB.Data;
using Moonglow_DB.Models;
using MySql.Data.MySqlClient;

namespace Moonglow_DB.Views
{
    public partial class OrdersWindow : Window
    {
        private readonly DatabaseContext _databaseContext;
        private List<Order> _allOrders;
        private Order _selectedOrder;

        public OrdersWindow(DatabaseContext databaseContext)
        {
            InitializeComponent();
            _databaseContext = databaseContext;
            LoadOrders();
            LoadFilters();
        }

        private void LoadOrders()
        {
            try
            {
                _allOrders = GetAllOrders();
                dgOrders.ItemsSource = _allOrders;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading orders: {ex.Message}", "Error");
            }
        }

                private List<Order> GetAllOrders()
        {
            var orders = new List<Order>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = @"
                SELECT o.Id, o.OrderNumber, o.CustomerId, o.EmployeeId, o.OrderType, o.Status, o.CreatedDate,
                       CASE 
                           WHEN o.CustomerId IS NULL THEN 'Anonymous Sale'
                           ELSE CONCAT(c.FirstName, ' ', c.LastName)
                       END as CustomerName,
                       CASE 
                           WHEN o.EmployeeId IS NULL THEN 'No Employee'
                           ELSE CONCAT(e.FirstName, ' ', e.LastName)
                       END as EmployeeName,
                       COUNT(oi.Id) as ItemCount,
                       SUM(oi.Quantity * oi.UnitPrice) as TotalAmount,
                       SUM(oi.Quantity * oi.UnitPrice * e.CommissionRate / 100) as CommissionAmount
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerId = c.Id
                LEFT JOIN Employees e ON o.EmployeeId = e.Id
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                GROUP BY o.Id, o.OrderNumber, o.CustomerId, o.EmployeeId, o.OrderType, o.Status, o.CreatedDate
                ORDER BY o.CreatedDate DESC";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt32(0),
                    OrderNumber = reader.GetString(1),
                    CustomerId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    EmployeeId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    OrderType = (OrderType)Enum.Parse(typeof(OrderType), reader.GetString(4)),
                    Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), reader.GetString(5)),
                    CreatedDate = reader.GetDateTime(6),
                    LastModified = reader.GetDateTime(6), // Use CreatedDate as LastModified for now
                    CustomerName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    EmployeeName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    ItemCount = reader.GetInt32(9),
                    TotalAmount = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10),
                    CommissionAmount = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                });
            }
            
            return orders;
        }

        private void LoadFilters()
        {
            // Load status filter
            var statuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList();
            cmbStatusFilter.ItemsSource = statuses;
            cmbStatusFilter.SelectedIndex = 0;

            // Load employee filter
            try
            {
                var employees = GetAllEmployees();
                cmbEmployeeFilter.ItemsSource = employees;
                cmbEmployeeFilter.DisplayMemberPath = "Name";
                cmbEmployeeFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error loading employees: {ex.Message}", "Error");
            }
        }

        private List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            
            using var connection = _databaseContext.GetConnection();
            var sql = "SELECT Id, FirstName, LastName FROM Employees WHERE IsActive = 1 ORDER BY FirstName, LastName";
            
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2)
                });
            }
            
            return employees;
        }

        private void btnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addOrderWindow = new AddOrderWindow(_databaseContext);
                if (addOrderWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError($"Error opening add order window: {ex.Message}", "Error");
            }
        }

        private void btnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                ErrorDialog.ShowInformation("Please select an order to edit.", "No Selection");
                return;
            }
            
            var editWindow = new EditOrderWindow(_databaseContext, _selectedOrder);
            if (editWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void btnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                ErrorDialog.ShowInformation("Please select an order to delete.", "No Selection");
                return;
            }

                        var result = ErrorDialog.ShowConfirmation($"Are you sure you want to delete order '{_selectedOrder.OrderNumber}'?",
                "Confirm Delete");
            
            if (result)
            {
                try
                {
                    DeleteOrder(_selectedOrder.Id);
                    LoadOrders();
                    ErrorDialog.ShowSuccess("Order deleted successfully.", "Success");
                }
                catch (Exception ex)
                {
                    ErrorDialog.ShowError($"Error deleting order: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteOrder(int orderId)
        {
            using var connection = _databaseContext.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete order items first
                var deleteItemsSql = "DELETE FROM OrderItems WHERE OrderId = @OrderId";
                using var deleteItemsCommand = new MySqlCommand(deleteItemsSql, connection);
                deleteItemsCommand.Transaction = transaction;
                deleteItemsCommand.Parameters.AddWithValue("@OrderId", orderId);
                deleteItemsCommand.ExecuteNonQuery();

                // Delete the order
                var deleteOrderSql = "DELETE FROM Orders WHERE Id = @Id";
                using var deleteOrderCommand = new MySqlCommand(deleteOrderSql, connection);
                deleteOrderCommand.Transaction = transaction;
                deleteOrderCommand.Parameters.AddWithValue("@Id", orderId);
                deleteOrderCommand.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbEmployeeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filteredOrders = _allOrders.AsEnumerable();

            // Apply status filter
            if (cmbStatusFilter.SelectedItem is OrderStatus selectedStatus)
            {
                filteredOrders = filteredOrders.Where(o => o.Status == selectedStatus);
            }

            // Apply employee filter
            if (cmbEmployeeFilter.SelectedItem is Employee selectedEmployee)
            {
                filteredOrders = filteredOrders.Where(o => o.EmployeeName == selectedEmployee.Name);
            }

            // Apply search filter
            var searchText = txtSearch.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredOrders = filteredOrders.Where(o => 
                    o.OrderNumber.ToLower().Contains(searchText) ||
                    o.CustomerName.ToLower().Contains(searchText) ||
                    o.EmployeeName.ToLower().Contains(searchText)
                );
            }

            dgOrders.ItemsSource = filteredOrders.ToList();
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder = dgOrders.SelectedItem as Order;
            btnEditOrder.IsEnabled = _selectedOrder != null;
            btnDeleteOrder.IsEnabled = _selectedOrder != null;
        }
    }
} 