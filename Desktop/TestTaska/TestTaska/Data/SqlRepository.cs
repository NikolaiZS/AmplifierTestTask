using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TestTaska.Models;

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

        public async Task<OperationResult<List<Product>>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            string sql = "SELECT ProductId, ProductName FROM Products ORDER BY ProductName";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1)
                            });
                        }
                    }
                }
                return OperationResult<List<Product>>.Success(products);
            }
            catch (Exception ex)
            {
                return OperationResult<List<Product>>.Failure($"Ошибка при получении списка товаров: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<ProductStockDisplay>>> GetCurrentStockAsync()
        {
            const string sql = @"
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

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var stocks = new List<ProductStockDisplay>();
                        while (await reader.ReadAsync())
                        {
                            stocks.Add(new ProductStockDisplay
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                TotalIn = reader.GetInt32(2),
                                TotalOut = reader.GetInt32(3)
                            });
                        }
                        return OperationResult<List<ProductStockDisplay>>.Success(stocks);
                    }
                }
            }
            catch (Exception ex)
            {
                return OperationResult<List<ProductStockDisplay>>.Failure($"Ошибка при получении остатков: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<StockReceipt>>> GetFilteredReceiptsAsync(DateTime startDate, DateTime endDate)
        {
            var receipts = new List<StockReceipt>();
            string sql = @"
                SELECT 
                    r.ReceiptId, r.ReceiptDate, r.Quantity, r.ProductId, p.ProductName 
                FROM StockReceipts r
                JOIN Products p ON r.ProductId = p.ProductId
                WHERE r.ReceiptDate >= @Start AND r.ReceiptDate < @EndPlusOneDay
                ORDER BY r.ReceiptDate DESC";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Start", startDate.Date);
                        command.Parameters.AddWithValue("@EndPlusOneDay", endDate.Date.AddDays(1));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                receipts.Add(new StockReceipt
                                {
                                    ReceiptId = reader.GetInt32(reader.GetOrdinal("ReceiptId")),
                                    ReceiptDate = reader.GetDateTime(reader.GetOrdinal("ReceiptDate")),
                                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                    Product = new Product { ProductName = reader.GetString(reader.GetOrdinal("ProductName")) }
                                });
                            }
                        }
                    }
                }
                return OperationResult<List<StockReceipt>>.Success(receipts);
            }
            catch (Exception ex)
            {
                return OperationResult<List<StockReceipt>>.Failure($"Ошибка при фильтрации приходов: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<StockOut>>> GetFilteredStockOutsAsync(DateTime startDate, DateTime endDate)
        {
            var stockOuts = new List<StockOut>();
            string sql = @"
                SELECT 
                    o.OutId, o.OutDate, o.Quantity, o.ProductId, p.ProductName 
                FROM StockOut o
                JOIN Products p ON o.ProductId = p.ProductId
                WHERE o.OutDate >= @Start AND o.OutDate < @EndPlusOneDay
                ORDER BY o.OutDate DESC";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Start", startDate.Date);
                        command.Parameters.AddWithValue("@EndPlusOneDay", endDate.Date.AddDays(1));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stockOuts.Add(new StockOut
                                {
                                    OutId = reader.GetInt32(reader.GetOrdinal("OutId")),
                                    OutDate = reader.GetDateTime(reader.GetOrdinal("OutDate")),
                                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                    Product = new Product { ProductName = reader.GetString(reader.GetOrdinal("ProductName")) }
                                });
                            }
                        }
                    }
                }
                return OperationResult<List<StockOut>>.Success(stockOuts);
            }
            catch (Exception ex)
            {
                return OperationResult<List<StockOut>>.Failure($"Ошибка при фильтрации уходов: {ex.Message}");
            }
        }


        public async Task<bool> CheckDuplicateProductAsync(string name)
        {
            string sql = "SELECT COUNT(*) FROM Products WHERE ProductName = @Name";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        var count = (int)await command.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DB Error in CheckDuplicateProductAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<OperationResult> AddProductAsync(string name)
        {
            string sql = "INSERT INTO Products (ProductName) VALUES (@Name)";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                OnProductsUpdated();
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при добавлении товара: {ex.Message}");
            }
        }

        public async Task<OperationResult> AddReceiptAsync(StockReceipt receipt)
        {
            string sql = "INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (@ProductId, @Date, @Quantity)";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", receipt.ProductId);
                        command.Parameters.AddWithValue("@Date", receipt.ReceiptDate.Date);
                        command.Parameters.AddWithValue("@Quantity", receipt.Quantity);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при добавлении прихода: {ex.Message}");
            }
        }

        public async Task<OperationResult> UpdateReceiptAsync(StockReceipt receipt)
        {
            string sql = "UPDATE StockReceipts SET ProductId = @ProductId, ReceiptDate = @Date, Quantity = @Quantity WHERE ReceiptId = @Id";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", receipt.ReceiptId);
                        command.Parameters.AddWithValue("@ProductId", receipt.ProductId);
                        command.Parameters.AddWithValue("@Date", receipt.ReceiptDate.Date);
                        command.Parameters.AddWithValue("@Quantity", receipt.Quantity);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при обновлении прихода: {ex.Message}");
            }
        }

        public async Task<OperationResult> DeleteReceiptAsync(int receiptId)
        {
            string sql = "DELETE FROM StockReceipts WHERE ReceiptId = @Id";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", receiptId);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при удалении прихода: {ex.Message}");
            }
        }

        public async Task<OperationResult> AddStockOutAsync(StockOut stockOut)
        {
            string sql = "INSERT INTO StockOut (ProductId, OutDate, Quantity) VALUES (@ProductId, @Date, @Quantity)";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", stockOut.ProductId);
                        command.Parameters.AddWithValue("@Date", stockOut.OutDate.Date);
                        command.Parameters.AddWithValue("@Quantity", stockOut.Quantity);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при добавлении ухода: {ex.Message}");
            }
        }

        public async Task<OperationResult> UpdateStockOutAsync(StockOut stockOut)
        {
            string sql = "UPDATE StockOut SET ProductId = @ProductId, OutDate = @Date, Quantity = @Quantity WHERE OutId = @Id";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", stockOut.OutId);
                        command.Parameters.AddWithValue("@ProductId", stockOut.ProductId);
                        command.Parameters.AddWithValue("@Date", stockOut.OutDate.Date);
                        command.Parameters.AddWithValue("@Quantity", stockOut.Quantity);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при обновлении ухода: {ex.Message}");
            }
        }

        public async Task<OperationResult> DeleteStockOutAsync(int outId)
        {
            string sql = "DELETE FROM StockOut WHERE OutId = @Id";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", outId);
                        await command.ExecuteNonQueryAsync();
                    }
                    OnStockMovementUpdated();
                    return OperationResult.Success();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при удалении ухода: {ex.Message}");
            }
        }

        public async Task<int> GetProductStockCountAsync(int productId)
        {
            const string sql = @"
        SELECT 
            (SELECT ISNULL(SUM(Quantity), 0) FROM StockReceipts WHERE ProductId = @id) - 
            (SELECT ISNULL(SUM(Quantity), 0) FROM StockOut WHERE ProductId = @id) 
        AS CurrentStock";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", productId);
                        var result = await command.ExecuteScalarAsync();
                        return result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подсчета остатка: {ex.Message}");
                return 0;
            }
        }
    }
}