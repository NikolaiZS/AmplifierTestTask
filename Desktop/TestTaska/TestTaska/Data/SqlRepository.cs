using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestTaska.Models;
using System.Diagnostics;

namespace TestTaska.Data
{
    public class SqlRepository
    {
        private string ConnectionString = "Server=localhost;Database=StockDB;Trusted_Connection=True;TrustServerCertificate=True";

        public static event Action ProductsUpdated;
        public static event Action StockMovementUpdated;


        private static void OnProductsUpdated()
        {
            ProductsUpdated?.Invoke();
        }

        private static void OnStockMovementUpdated()
        {
            StockMovementUpdated?.Invoke();
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();
            string sql = "SELECT ProductId, ProductName FROM Products ORDER BY ProductName";

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1)
                        });
                    }
                }
            }
            return products;
        }

        public bool CheckDuplicateProduct(string name)
        {
            string sql = "SELECT COUNT(*) FROM Products WHERE ProductName = @Name";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public void AddProduct(string name)
        {
            string sql = "INSERT INTO Products (ProductName) VALUES (@Name)";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.ExecuteNonQuery();
                }
                OnProductsUpdated();
            }
        }

        public List<ProductStockDisplay> GetCurrentStock()
        {
            var stockList = new List<ProductStockDisplay>();

            string sql = @"
                SELECT 
                    p.ProductId, 
                    p.ProductName, 
                    ISNULL(r.TotalIn, 0) AS TotalIn,
                    ISNULL(o.TotalOut, 0) AS TotalOut
                FROM Products p
                LEFT JOIN (
                    SELECT 
                        ProductId, 
                        SUM(Quantity) AS TotalIn
                    FROM StockReceipts 
                    GROUP BY ProductId
                ) r ON p.ProductId = r.ProductId
                LEFT JOIN (
                    SELECT 
                        ProductId, 
                        SUM(Quantity) AS TotalOut
                    FROM StockOut 
                    GROUP BY ProductId
                ) o ON p.ProductId = o.ProductId
                ORDER BY p.ProductName";
                

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stockList.Add(new ProductStockDisplay
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            TotalIn = reader.GetInt32(2),
                            TotalOut = reader.GetInt32(3)
                        });
                    }
                }
            }
            return stockList;
        }

        public List<StockReceipt> GetFilteredReceipts(DateTime startDate, DateTime endDate)
        {
            var receipts = new List<StockReceipt>();
            string sql = @"
                SELECT 
                    r.ReceiptId, r.ReceiptDate, r.Quantity, r.ProductId, p.ProductName 
                FROM StockReceipts r
                JOIN Products p ON r.ProductId = p.ProductId
                WHERE r.ReceiptDate >= @Start AND r.ReceiptDate < @EndPlusOneDay
                ORDER BY r.ReceiptDate DESC";

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Start", startDate.Date);
                    command.Parameters.AddWithValue("@EndPlusOneDay", endDate.Date.AddDays(1));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            receipts.Add(new StockReceipt
                            {
                                ReceiptId = reader.GetInt32("ReceiptId"),
                                ReceiptDate = reader.GetDateTime("ReceiptDate"),
                                Quantity = reader.GetInt32("Quantity"),
                                ProductId = reader.GetInt32("ProductId"),
                                Product = new Product { ProductName = reader.GetString("ProductName") }
                            });
                        }
                    }
                }
            }
            return receipts;
        }

        public void AddReceipt(StockReceipt receipt)
        {
            string sql = "INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (@ProductId, @Date, @Quantity)";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", receipt.ProductId);
                    command.Parameters.AddWithValue("@Date", receipt.ReceiptDate.Date);
                    command.Parameters.AddWithValue("@Quantity", receipt.Quantity);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }

        public void UpdateReceipt(StockReceipt receipt)
        {
            string sql = "UPDATE StockReceipts SET ProductId = @ProductId, ReceiptDate = @Date, Quantity = @Quantity WHERE ReceiptId = @Id";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", receipt.ReceiptId);
                    command.Parameters.AddWithValue("@ProductId", receipt.ProductId);
                    command.Parameters.AddWithValue("@Date", receipt.ReceiptDate.Date);
                    command.Parameters.AddWithValue("@Quantity", receipt.Quantity);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }

        public void DeleteReceipt(int receiptId)
        {
            string sql = "DELETE FROM StockReceipts WHERE ReceiptId = @Id";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", receiptId);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }

        public List<StockOut> GetFilteredStockOuts(DateTime startDate, DateTime endDate)
        {
            var stockOuts = new List<StockOut>();
            string sql = @"
                SELECT 
                    o.OutId, o.OutDate, o.Quantity, o.ProductId, p.ProductName 
                FROM StockOut o
                JOIN Products p ON o.ProductId = p.ProductId
                WHERE o.OutDate >= @Start AND o.OutDate < @EndPlusOneDay
                ORDER BY o.OutDate DESC";

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Start", startDate.Date);
                    command.Parameters.AddWithValue("@EndPlusOneDay", endDate.Date.AddDays(1));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stockOuts.Add(new StockOut
                            {
                                OutId = reader.GetInt32("OutId"),
                                OutDate = reader.GetDateTime("OutDate"),
                                Quantity = reader.GetInt32("Quantity"),
                                ProductId = reader.GetInt32("ProductId"),
                                Product = new Product { ProductName = reader.GetString("ProductName") }
                            });
                        }
                    }
                }
            }
            return stockOuts;
        }

        public void AddStockOut(StockOut stockOut)
        {
            string sql = "INSERT INTO StockOut (ProductId, OutDate, Quantity) VALUES (@ProductId, @Date, @Quantity)";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", stockOut.ProductId);
                    command.Parameters.AddWithValue("@Date", stockOut.OutDate.Date);
                    command.Parameters.AddWithValue("@Quantity", stockOut.Quantity);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }

        public void UpdateStockOut(StockOut stockOut)
        {
            string sql = "UPDATE StockOut SET ProductId = @ProductId, OutDate = @Date, Quantity = @Quantity WHERE OutId = @Id";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", stockOut.OutId);
                    command.Parameters.AddWithValue("@ProductId", stockOut.ProductId);
                    command.Parameters.AddWithValue("@Date", stockOut.OutDate.Date);
                    command.Parameters.AddWithValue("@Quantity", stockOut.Quantity);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }

        public void DeleteStockOut(int outId)
        {
            string sql = "DELETE FROM StockOut WHERE OutId = @Id";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", outId);
                    command.ExecuteNonQuery();
                }
                OnStockMovementUpdated();
            }
        }
    }
}
