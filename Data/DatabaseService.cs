using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuOrderMAUI.Models;
using MySqlConnector;
using MenuOrderMAUI.Data;

namespace MenuOrderMAUI.Data
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=localhost;Database=menu_order;UserID=root;Password=password;";
    
        public MySqlConnection GetConnection() => new MySqlConnection(_connectionString);
        public async Task<List<MenuItems>> GetMenuItemsAsync()
        {
            var menuItems = new List<MenuItems>();
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand("SELECT * FROM MenuItems;", connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    menuItems.Add(new MenuItems
                    {
                        ItemID = reader.GetInt32(0),
                        ItemName = reader.GetString(1),
                        Price = reader.GetDecimal(2)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMenuItemsAsync: {ex.Message}");
            }
            if (!menuItems.Any())
            {
                Console.WriteLine("No menu items found in the database.");
            }
            return menuItems;
        }
        public async Task AddMenuItemAsync(string itemName, decimal price)
        {
            try
            {
                string query = "INSERT INTO MenuItems (ItemName, Price) VALUES (@ItemName, @Price);";
                using var connection = GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ItemName", itemName);
                command.Parameters.AddWithValue("@Price", price);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Menu item '{itemName}' added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding menu item: {ex.Message}");
            }
        }
        public async Task<List<Orders>> GetOrdersAsync()
        {
            var orders = new List<Orders>();
            try
            {
                string query = @"
                    SELECT o.OrderID, m.ItemName, o.Quantity, o.OrderDate
                    FROM Orders o
                    JOIN MenuItems m ON o.ItemID = m.ItemID;";
                using var connection = GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    orders.Add(new Orders
                    {
                        OrderID = reader.GetInt32("OrderID"),
                        ItemName = reader.GetString("ItemName"),
                        Quantity = reader.GetInt32("Quantity"),
                        OrderDate = reader.GetDateTime("OrderDate")
                    });
                }
                Console.WriteLine("Order placed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlaceOrderAsync: {ex.Message}");
            }
            return orders;
        }

        public async Task PlaceOrderAsync(int itemID, int quantity)
        {
            try
            {
                string query = "INSERT INTO Orders (ItemID, Quantity) VALUES (@ItemID, @Quantity);";
                using var connection = GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ItemID", itemID);
                command.Parameters.AddWithValue("@Quantity", quantity);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlaceOrderAsync: {ex.Message}");
            }
        }
        public async Task<List<Receipts>> GetReceiptsAsync()
        {
            var receipts = new List<Receipts>();
            try
            {
                string query = @"
                    SELECT r.ReceiptID, r.OrderID, r.TotalBill, r.GeneratedDate
                    FROM Receipts r
                    JOIN Orders o ON r.OrderID = o.OrderID;";
                using var connection = GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    receipts.Add(new Receipts
                    {
                        ReceiptID = reader.GetInt32("ReceiptID"),
                        OrderID = reader.GetInt32("OrderID"),
                        TotalBill = reader.GetDecimal("TotalBill"),
                        GenerateDate = reader.GetDateTime("GeneratedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetReceiptsAsync: {ex.Message}");
            }
            return receipts;
        }

        public async Task PlaceReceiptAsync(int orderID, int totalbill)
        {
            string query = "INSERT INTO Receipts (OrderID, TotalBill) VALUES (@OrderID, @TotalBill);";
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderID", orderID);
            command.Parameters.AddWithValue("@TotalBill", totalbill);
            await command.ExecuteNonQueryAsync();
        }

        public async Task GenerateReceiptAsync(int orderId)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                // Step 1: Calculate Total Bill for the Order
                string calculateTotalQuery = @"
                     SELECT SUM(o.Quantity * m.Price) AS TotalBill
                     FROM Orders o
                     JOIN MenuItems m ON o.ItemID = m.ItemID
                     WHERE o.OrderID = @OrderID;";

                decimal totalBill = 0;
                using var calculateCommand = new MySqlCommand(calculateTotalQuery, connection);
                calculateCommand.Parameters.AddWithValue("@OrderID", orderId);

                using var calculateReader = await calculateCommand.ExecuteReaderAsync();
                if (await calculateReader.ReadAsync() && !DBNull.Value.Equals(calculateReader["TotalBill"]))
                {
                    totalBill = calculateReader.GetDecimal("TotalBill");
                }
                calculateReader.Close();

                if (totalBill > 0)
                {
                    // Step 2: Insert the Receipt into the Receipts Table
                    string insertReceiptQuery = @"
                INSERT INTO Receipts (OrderID, TotalBill)
                VALUES (@OrderID, @TotalBill);";

                    using var insertCommand = new MySqlCommand(insertReceiptQuery, connection);
                    insertCommand.Parameters.AddWithValue("@OrderID", orderId);
                    insertCommand.Parameters.AddWithValue("@TotalBill", totalBill);

                    await insertCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    Console.WriteLine("Order not found or no items in the order.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Payments>> GetPaymentsAsync()
        {
            var payments = new List<Payments>();
            string query = @"
        SELECT p.PaymentID, p.OrderID, p.PaymentMethod, p.PaymentAmount, p.ChangeAmount, p.PaymentDate, r.TotalBill
        FROM Payments p
        JOIN Receipts r ON p.OrderID = r.OrderID;"; // Add TotalBill from Receipts table

            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                payments.Add(new Payments
                {
                    PaymentID = reader.GetInt32("PaymentID"),
                    OrderID = reader.GetInt32("OrderID"),
                    PaymentMethod = reader.GetString("PaymentMethod"),
                    PaymentAmount = reader.GetDecimal("PaymentAmount"),
                    ChangeAmount = reader.GetDecimal("ChangeAmount"),
                    PaymentDate = reader.GetDateTime("PaymentDate"),
                    TotalBill = reader.GetDecimal("TotalBill") // Map TotalBill from Receipts table
                });
            }
            return payments;
        }
        public async Task AddPaymentAsync(int orderID, string paymentMethod, decimal paymentAmount)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                // Step 1: Check if a payment for this OrderID already exists
                string checkQuery = "SELECT COUNT(*) FROM Payments WHERE OrderID = @OrderID;";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@OrderID", orderID);
                var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (existingCount > 0)
                {
                    throw new InvalidOperationException($"A payment for OrderID: {orderID} already exists.");
                }

                // Step 2: Insert the payment into the Payments table
                string insertPaymentQuery = @"
            INSERT INTO Payments (OrderID, PaymentMethod, PaymentAmount)
            VALUES (@OrderID, @PaymentMethod, @PaymentAmount);";

                using var insertCommand = new MySqlCommand(insertPaymentQuery, connection);
                insertCommand.Parameters.AddWithValue("@OrderID", orderID);
                insertCommand.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                insertCommand.Parameters.AddWithValue("@PaymentAmount", paymentAmount);
                await insertCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"Payment added successfully for OrderID: {orderID}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddPaymentAsync: {ex.Message}");
                throw;
            }
        }
    }
}
