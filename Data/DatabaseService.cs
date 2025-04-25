using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuOrderMAUI.Models;
using MySqlConnector;

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
    }
}
