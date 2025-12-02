--Десктоп предполагает что база данных называется StockDB, но можно поменять в классе SqlRepository в поле ConnectionString, параметр Database

-- 1. Таблица товаров
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(255) NOT NULL UNIQUE 
);

-- 2. Таблица Прихода
CREATE TABLE StockReceipts (
    ReceiptId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    ReceiptDate DATE NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    
    CONSTRAINT FK_Receipt_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- 3. Таблица Расхода
CREATE TABLE StockOut (
    OutId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    OutDate DATE NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    
    CONSTRAINT FK_Out_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);
