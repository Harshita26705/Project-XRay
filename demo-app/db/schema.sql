-- CGOne demo schema (SQL Server dialect).
-- Synthetic data model used to exercise X-Ray's SQL parser.

CREATE TABLE [dbo].[Users]
(
    [Id]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Email]       NVARCHAR(256) NOT NULL,
    [DisplayName] NVARCHAR(128) NOT NULL
);

CREATE TABLE [dbo].[InventoryItems]
(
    [Id]       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Sku]      NVARCHAR(64) NOT NULL,
    [Quantity] INT NOT NULL
);

CREATE TABLE [dbo].[Orders]
(
    [Id]     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [Total]  DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(32) NOT NULL,
    CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);

CREATE TABLE [dbo].[PaymentTransactions]
(
    [Id]        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OrderId]   INT NOT NULL,
    [Amount]    DECIMAL(18,2) NOT NULL,
    [Currency]  NVARCHAR(3) NOT NULL,
    [Status]    NVARCHAR(32) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    CONSTRAINT [FK_PaymentTransactions_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id])
);
