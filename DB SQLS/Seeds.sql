INSERT INTO Products (ProductName) VALUES 
('Пластик ABS'),         
('Датчик температуры NTC'), 
('Плата Arduino Uno'),    
('Кабель USB-C');         


INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (1, DATEADD(day, -30, GETDATE()), 100);
INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (1, DATEADD(day, -10, GETDATE()), 50);

INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (2, DATEADD(day, -25, GETDATE()), 200);

INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (4, DATEADD(day, -5, GETDATE()), 300);

INSERT INTO StockOut (ProductId, OutDate, Quantity) VALUES (1, DATEADD(day, -20, GETDATE()), 30);
INSERT INTO StockOut (ProductId, OutDate, Quantity) VALUES (1, DATEADD(day, -5, GETDATE()), 20);

INSERT INTO StockOut (ProductId, OutDate, Quantity) VALUES (2, DATEADD(day, -15, GETDATE()), 150);

INSERT INTO StockReceipts (ProductId, ReceiptDate, Quantity) VALUES (3, DATEADD(day, -7, GETDATE()), 10);