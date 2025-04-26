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

        public async Task GenerateReceiptAsync(int orderID)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                // Step 1: Fetch the maximum OrderID from the Orders table
                string maxOrderQuery = "SELECT MAX(OrderID) FROM Orders;";
                using var maxOrderCommand = new MySqlCommand(maxOrderQuery, connection);
                var maxOrderID = Convert.ToInt32(await maxOrderCommand.ExecuteScalarAsync());

                // Step 2: Validate the provided OrderID
                if (orderID > maxOrderID)
                {
                    throw new InvalidOperationException($"Cannot generate receipt. OrderID {orderID} is not save in database yet.");
                }

                // Step 3: Check if the OrderID exists in the Orders table
                string checkOrderQuery = "SELECT COUNT(*) FROM Orders WHERE OrderID = @OrderID;";
                using var checkOrderCommand = new MySqlCommand(checkOrderQuery, connection);
                checkOrderCommand.Parameters.AddWithValue("@OrderID", orderID);
                var orderExists = Convert.ToInt32(await checkOrderCommand.ExecuteScalarAsync());

                if (orderExists == 0)
                {
                    throw new InvalidOperationException($"OrderID {orderID} does not exist.");
                }

                // Step 4: Insert receipt into the Receipts table
                string insertReceiptQuery = @"
            INSERT INTO Receipts (OrderID, TotalBill, GenerateDate)
            SELECT OrderID, SUM(Quantity * Price), NOW()
            FROM Orders
            JOIN MenuItems ON Orders.ItemID = MenuItems.ItemID
            WHERE Orders.OrderID = @OrderID
            GROUP BY Orders.OrderID;";

                using var insertReceiptCommand = new MySqlCommand(insertReceiptQuery, connection);
                insertReceiptCommand.Parameters.AddWithValue("@OrderID", orderID);
                await insertReceiptCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"Receipt generated successfully for OrderID {orderID}.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Validation Error: {ex.Message}");
                throw;
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
                    TotalBill = reader.GetDecimal("TotalBill"),
                    ChangeAmount = reader.GetDecimal("ChangeAmount"),
                    PaymentDate = reader.GetDateTime("PaymentDate")
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

                // Step 1: Fetch TotalBill for the given OrderID
                string getTotalBillQuery = "SELECT TotalBill FROM Receipts WHERE OrderID = @OrderID;";
                decimal totalBill = 0;

                using (var command = new MySqlCommand(getTotalBillQuery, connection))
                {
                    command.Parameters.AddWithValue("@OrderID", orderID);
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        totalBill = reader.GetDecimal("TotalBill");
                    }
                    else
                    {
                        throw new InvalidOperationException($"No receipt found for OrderID: {orderID}");
                    }
                }

                if (paymentAmount < totalBill)
                {
                    throw new InvalidOperationException($"Payment amount is insufficient. TotalBill: {totalBill:C}, PaymentAmount is only: {paymentAmount:C}");
                }

                decimal changeAmount = paymentAmount - totalBill;

                string insertPaymentQuery = @"
            INSERT INTO Payments (OrderID, PaymentMethod, PaymentAmount, ChangeAmount)
            VALUES (@OrderID, @PaymentMethod, @PaymentAmount, @ChangeAmount);";

                using var insertCommand = new MySqlCommand(insertPaymentQuery, connection);
                insertCommand.Parameters.AddWithValue("@OrderID", orderID);
                insertCommand.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                insertCommand.Parameters.AddWithValue("@PaymentAmount", paymentAmount);
                insertCommand.Parameters.AddWithValue("@ChangeAmount", changeAmount);
                await insertCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"Payment added successfully for OrderID: {orderID}, ChangeAmount: {changeAmount}");
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
