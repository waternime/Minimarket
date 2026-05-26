IF DB_ID(N'MinimarketDb') IS NULL
BEGIN
    CREATE DATABASE [MinimarketDb];
END;
GO

USE [MinimarketDb];
GO

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
GO

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
GO

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
GO

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
GO

IF COL_LENGTH('dbo.Products', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.Products ADD DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Products_Discount DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash)
    VALUES (N'admin', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9');
END;
GO
