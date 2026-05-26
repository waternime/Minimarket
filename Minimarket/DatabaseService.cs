using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Minimarket
{
    internal sealed class DatabaseService
    {
        private const string DatabaseName = "MinimarketDb";
        private readonly string connectionString;

        public DatabaseService()
        {
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["MinimarketDb"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new InvalidOperationException("Connection string 'MinimarketDb' belum ada di App.config.");
            }

            connectionString = setting.ConnectionString;
        }

        public void Initialize()
        {
            EnsureDatabase();

            using (SqlConnection connection = OpenConnection())
            {
                ExecuteNonQuery(connection, @"
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
        PasswordHash NVARCHAR(64) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
END;

IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL CONSTRAINT UQ_Products_Code UNIQUE,
        Name NVARCHAR(120) NOT NULL,
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Products_Discount DEFAULT 0 CONSTRAINT CK_Products_Discount CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
        Stock INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0 CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
END;

IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        TransactionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
        InvoiceNo NVARCHAR(30) NOT NULL CONSTRAINT UQ_Transactions_InvoiceNo UNIQUE,
        TransactionDate DATETIME2 NOT NULL CONSTRAINT DF_Transactions_Date DEFAULT SYSDATETIME(),
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaidAmount DECIMAL(18,2) NOT NULL,
        ChangeAmount DECIMAL(18,2) NOT NULL
    );
END;

IF OBJECT_ID('dbo.TransactionItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionItems
    (
        TransactionItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TransactionItems PRIMARY KEY,
        TransactionId INT NOT NULL,
        ProductId INT NULL,
        ProductCode NVARCHAR(50) NULL,
        ProductName NVARCHAR(120) NOT NULL,
        Qty INT NOT NULL CONSTRAINT CK_TransactionItems_Qty CHECK (Qty > 0),
        OriginalPrice DECIMAL(18,2) NOT NULL,
        DiscountPercent DECIMAL(5,2) NOT NULL,
        FinalPrice DECIMAL(18,2) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_TransactionItems_Transactions FOREIGN KEY (TransactionId) REFERENCES dbo.Transactions(TransactionId) ON DELETE CASCADE,
        CONSTRAINT FK_TransactionItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId) ON DELETE SET NULL
    );
END;

IF COL_LENGTH('dbo.Products', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.Products ADD DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Products_Discount DEFAULT 0;
END;
");

                using (SqlCommand command = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM dbo.Users) INSERT INTO dbo.Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);", connection))
                {
                    command.Parameters.AddWithValue("@Username", "admin");
                    command.Parameters.AddWithValue("@PasswordHash", HashPassword("admin123"));
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool ValidateLogin(string username, string password)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM dbo.Users WHERE Username = @Username AND PasswordHash = @PasswordHash;", connection))
            {
                command.Parameters.AddWithValue("@Username", username.Trim());
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public int GetUserId(string username)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand("SELECT TOP 1 UserId FROM dbo.Users WHERE Username = @Username;", connection))
            {
                command.Parameters.AddWithValue("@Username", username.Trim());
                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public void UpdateAccount(int userId, string username, string password)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET Username = @Username,
    PasswordHash = @PasswordHash,
    UpdatedAt = SYSUTCDATETIME()
WHERE UserId = @UserId;", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Username", username.Trim());
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                command.ExecuteNonQuery();
            }
        }

        public List<Product> GetProducts()
        {
            List<Product> products = new List<Product>();

            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
SELECT ProductId, Code, Name, Price, DiscountPercent, Stock
FROM dbo.Products
ORDER BY Name;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    products.Add(ReadProduct(reader));
                }
            }

            return products;
        }

        public Product GetProductByCode(string code)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
SELECT ProductId, Code, Name, Price, DiscountPercent, Stock
FROM dbo.Products
WHERE Code = @Code;", connection))
            {
                command.Parameters.AddWithValue("@Code", code.Trim());

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadProduct(reader) : null;
                }
            }
        }

        public void AddProduct(Product product)
        {
            string code = string.IsNullOrWhiteSpace(product.Code) ? GenerateProductCode() : product.Code.Trim();

            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.Products (Code, Name, Price, DiscountPercent, Stock)
VALUES (@Code, @Name, @Price, @DiscountPercent, @Stock);", connection))
            {
                AddProductParameters(command, product, code);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateProduct(Product product)
        {
            string code = string.IsNullOrWhiteSpace(product.Code) ? GenerateProductCode() : product.Code.Trim();

            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Products
SET Code = @Code,
    Name = @Name,
    Price = @Price,
    DiscountPercent = @DiscountPercent,
    Stock = @Stock,
    UpdatedAt = SYSUTCDATETIME()
WHERE ProductId = @ProductId;", connection))
            {
                command.Parameters.AddWithValue("@ProductId", product.ProductId);
                AddProductParameters(command, product, code);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteProduct(int productId)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand("DELETE FROM dbo.Products WHERE ProductId = @ProductId;", connection))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                command.ExecuteNonQuery();
            }
        }

        public int SaveTransaction(List<CartItem> cartItems, decimal paidAmount)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                throw new InvalidOperationException("Keranjang masih kosong.");
            }

            decimal total = 0m;
            foreach (CartItem item in cartItems)
            {
                total += item.Subtotal;
            }

            total = Money.Round(total);
            decimal change = Money.Round(paidAmount - total);
            if (change < 0)
            {
                throw new InvalidOperationException("Uang diterima belum cukup.");
            }

            using (SqlConnection connection = OpenConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (CartItem item in cartItems)
                    {
                        using (SqlCommand stockCommand = new SqlCommand("SELECT Stock FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @ProductId;", connection, transaction))
                        {
                            stockCommand.Parameters.AddWithValue("@ProductId", item.Product.ProductId);
                            object result = stockCommand.ExecuteScalar();
                            if (result == null || result == DBNull.Value)
                            {
                                throw new InvalidOperationException("Produk tidak ditemukan: " + item.Product.Name);
                            }

                            int stock = Convert.ToInt32(result);
                            if (stock < item.Qty)
                            {
                                throw new InvalidOperationException("Stok " + item.Product.Name + " tidak cukup. Sisa stok: " + stock + ".");
                            }
                        }
                    }

                    int transactionId;
                    string invoiceNo = "TRX" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.Transactions (InvoiceNo, TotalAmount, PaidAmount, ChangeAmount)
VALUES (@InvoiceNo, @TotalAmount, @PaidAmount, @ChangeAmount);
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@InvoiceNo", invoiceNo);
                        command.Parameters.AddWithValue("@TotalAmount", total);
                        command.Parameters.AddWithValue("@PaidAmount", paidAmount);
                        command.Parameters.AddWithValue("@ChangeAmount", change);
                        transactionId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    foreach (CartItem item in cartItems)
                    {
                        using (SqlCommand itemCommand = new SqlCommand(@"
INSERT INTO dbo.TransactionItems
    (TransactionId, ProductId, ProductCode, ProductName, Qty, OriginalPrice, DiscountPercent, FinalPrice, Subtotal)
VALUES
    (@TransactionId, @ProductId, @ProductCode, @ProductName, @Qty, @OriginalPrice, @DiscountPercent, @FinalPrice, @Subtotal);

UPDATE dbo.Products
SET Stock = Stock - @Qty,
    UpdatedAt = SYSUTCDATETIME()
WHERE ProductId = @ProductId;", connection, transaction))
                        {
                            itemCommand.Parameters.AddWithValue("@TransactionId", transactionId);
                            itemCommand.Parameters.AddWithValue("@ProductId", item.Product.ProductId);
                            itemCommand.Parameters.AddWithValue("@ProductCode", item.Product.Code);
                            itemCommand.Parameters.AddWithValue("@ProductName", item.Product.Name);
                            itemCommand.Parameters.AddWithValue("@Qty", item.Qty);
                            itemCommand.Parameters.AddWithValue("@OriginalPrice", item.Product.Price);
                            itemCommand.Parameters.AddWithValue("@DiscountPercent", item.Product.DiscountPercent);
                            itemCommand.Parameters.AddWithValue("@FinalPrice", item.Product.FinalPrice);
                            itemCommand.Parameters.AddWithValue("@Subtotal", item.Subtotal);
                            itemCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return transactionId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<SalesTransaction> GetTransactions()
        {
            List<SalesTransaction> transactions = new List<SalesTransaction>();

            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
SELECT TransactionId, InvoiceNo, TransactionDate, TotalAmount, PaidAmount, ChangeAmount
FROM dbo.Transactions
ORDER BY TransactionDate DESC;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    transactions.Add(new SalesTransaction
                    {
                        TransactionId = reader.GetInt32(0),
                        InvoiceNo = reader.GetString(1),
                        TransactionDate = reader.GetDateTime(2),
                        TotalAmount = reader.GetDecimal(3),
                        PaidAmount = reader.GetDecimal(4),
                        ChangeAmount = reader.GetDecimal(5)
                    });
                }
            }

            return transactions;
        }

        public SalesTransaction GetTransaction(int transactionId)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
SELECT TransactionId, InvoiceNo, TransactionDate, TotalAmount, PaidAmount, ChangeAmount
FROM dbo.Transactions
WHERE TransactionId = @TransactionId;", connection))
            {
                command.Parameters.AddWithValue("@TransactionId", transactionId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new SalesTransaction
                    {
                        TransactionId = reader.GetInt32(0),
                        InvoiceNo = reader.GetString(1),
                        TransactionDate = reader.GetDateTime(2),
                        TotalAmount = reader.GetDecimal(3),
                        PaidAmount = reader.GetDecimal(4),
                        ChangeAmount = reader.GetDecimal(5)
                    };
                }
            }
        }

        public List<SaleItem> GetTransactionItems(int transactionId)
        {
            List<SaleItem> items = new List<SaleItem>();

            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand(@"
SELECT ProductId, ProductCode, ProductName, Qty, OriginalPrice, DiscountPercent, FinalPrice, Subtotal
FROM dbo.TransactionItems
WHERE TransactionId = @TransactionId
ORDER BY TransactionItemId;", connection))
            {
                command.Parameters.AddWithValue("@TransactionId", transactionId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SaleItem
                        {
                            ProductId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            ProductCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            ProductName = reader.GetString(2),
                            Qty = reader.GetInt32(3),
                            OriginalPrice = reader.GetDecimal(4),
                            DiscountPercent = reader.GetDecimal(5),
                            FinalPrice = reader.GetDecimal(6),
                            Subtotal = reader.GetDecimal(7)
                        });
                    }
                }
            }

            return items;
        }

        public void DeleteHistory()
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand("DELETE FROM dbo.TransactionItems; DELETE FROM dbo.Transactions;", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        public void DeleteTransaction(int transactionId)
        {
            using (SqlConnection connection = OpenConnection())
            using (SqlCommand command = new SqlCommand("DELETE FROM dbo.Transactions WHERE TransactionId = @TransactionId;", connection))
            {
                command.Parameters.AddWithValue("@TransactionId", transactionId);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureDatabase()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            string database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? DatabaseName : builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @Sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
    EXEC sp_executesql @Sql;
END;", connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", database);
                    command.ExecuteNonQuery();
                }
            }
        }

        private SqlConnection OpenConnection()
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static Product ReadProduct(IDataRecord reader)
        {
            return new Product
            {
                ProductId = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                DiscountPercent = reader.GetDecimal(4),
                Stock = reader.GetInt32(5)
            };
        }

        private static void AddProductParameters(SqlCommand command, Product product, string code)
        {
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", product.Name.Trim());
            command.Parameters.AddWithValue("@Price", Money.Round(product.Price));
            command.Parameters.AddWithValue("@DiscountPercent", product.DiscountPercent);
            command.Parameters.AddWithValue("@Stock", product.Stock);
        }

        private string GenerateProductCode()
        {
            return "PRD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
