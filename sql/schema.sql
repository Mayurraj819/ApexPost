IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [Controller] nvarchar(100) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [StaffId] nvarchar(100) NULL,
        [Role] nvarchar(40) NULL,
        [BranchId] nvarchar(40) NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [StatusCode] int NOT NULL,
        [ElapsedMilliseconds] float NOT NULL,
        [SafeRoutes] nvarchar(1000) NOT NULL,
        [Outcome] nvarchar(40) NOT NULL,
        [ExceptionType] nvarchar(200) NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Audit_Status] CHECK ([StatusCode] BETWEEN 100 AND 599)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [IdentityVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentType] nvarchar(40) NOT NULL,
        [MaskedNumber] varchar(7) NOT NULL,
        [Fingerprint] varchar(64) NOT NULL,
        [IsVerified] bit NOT NULL,
        [Method] nvarchar(60) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_IdentityVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Identity_Masked] CHECK (LEFT([MaskedNumber],3) = '***' AND LEN([MaskedNumber]) BETWEEN 4 AND 7)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [Id] int NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Regions] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(40) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Regions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL,
        [Name] nvarchar(40) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [ShippingZones] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(40) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ShippingZones] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [Sku] varchar(40) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Category] int NOT NULL,
        [Weight] decimal(18,3) NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [UnitPriceAmount] decimal(18,2) NOT NULL,
        [UnitPriceCurrency] varchar(3) NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Product_Price] CHECK ([UnitPriceAmount] >= 0),
        CONSTRAINT [CK_Product_Weight] CHECK ([Weight] IS NULL OR [Weight] >= 0),
        CONSTRAINT [FK_Products_ProductCategories_Category] FOREIGN KEY ([Category]) REFERENCES [ProductCategories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(40) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [OpeningHours_OpensAt] time NOT NULL,
        [OpeningHours_ClosesAt] time NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [RegionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Branches_Regions_RegionId] FOREIGN KEY ([RegionId]) REFERENCES [Regions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [ShippingRates] (
        [Id] uniqueidentifier NOT NULL,
        [ZoneId] uniqueidentifier NOT NULL,
        [ServiceLevel] int NOT NULL,
        [BaseRate] decimal(18,2) NOT NULL,
        [PerKilogram] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ShippingRates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Rate_Amounts] CHECK ([BaseRate] > 0 AND [PerKilogram] >= 0),
        CONSTRAINT [FK_ShippingRates_ShippingZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ShippingZones] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [BranchStock] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_BranchStock] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Stock_Quantity] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_BranchStock_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
        CONSTRAINT [FK_BranchStock_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Counters] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Counters] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Counters_Id_BranchId] UNIQUE ([Id], [BranchId]),
        CONSTRAINT [CK_Counter_Number] CHECK ([Number] > 0),
        CONSTRAINT [FK_Counters_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Staff] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [EmployeeNumber] varchar(40) NOT NULL,
        [FullName] nvarchar(120) NOT NULL,
        [Email] varchar(254) NOT NULL,
        [Role] int NOT NULL,
        [IdentityDocument_DocumentType] nvarchar(40) NULL,
        [IdentityDocument_MaskedNumber] varchar(7) NULL,
        [IdentityDocument_Fingerprint] varchar(64) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Staff_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
        CONSTRAINT [FK_Staff_Roles_Role] FOREIGN KEY ([Role]) REFERENCES [Roles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [StockAdjustments] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Delta] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_StockAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Adjustment_Delta] CHECK ([Delta] <> 0),
        CONSTRAINT [FK_StockAdjustments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
        CONSTRAINT [FK_StockAdjustments_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [CounterTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [CounterId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CounterTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Transaction_Status] CHECK ([Status] IN (0,1,2,3)),
        CONSTRAINT [FK_CounterTransactions_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
        CONSTRAINT [FK_CounterTransactions_Counters_CounterId_BranchId] FOREIGN KEY ([CounterId], [BranchId]) REFERENCES [Counters] ([Id], [BranchId]),
        CONSTRAINT [FK_CounterTransactions_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [MoneyOrders] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [PayeeName] nvarchar(120) NOT NULL,
        [IdentityVerificationId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [OrderAmount] decimal(18,2) NOT NULL,
        [OrderCurrency] varchar(3) NOT NULL,
        CONSTRAINT [PK_MoneyOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MoneyOrder_Amount] CHECK ([OrderAmount] > 0),
        CONSTRAINT [FK_MoneyOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
        CONSTRAINT [FK_MoneyOrders_IdentityVerifications_IdentityVerificationId] FOREIGN KEY ([IdentityVerificationId]) REFERENCES [IdentityVerifications] ([Id]),
        CONSTRAINT [FK_MoneyOrders_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [PaidAtUtc] datetime2 NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [PaidCurrency] varchar(3) NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payment_Amount] CHECK ([PaidAmount] > 0),
        CONSTRAINT [FK_Payments_CounterTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CounterTransactions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [Shipments] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [ZoneId] uniqueidentifier NOT NULL,
        [ServiceLevel] int NOT NULL,
        [Weight] decimal(18,3) NOT NULL,
        [RecipientName] nvarchar(120) NOT NULL,
        [Address_Street] nvarchar(200) NOT NULL,
        [Address_City] nvarchar(100) NOT NULL,
        [Address_PostalCode] varchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [ChargeAmount] decimal(18,2) NOT NULL,
        [ChargeCurrency] varchar(3) NOT NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Shipment_Weight] CHECK ([Weight] > 0 AND [Weight] <= 30000),
        CONSTRAINT [FK_Shipments_CounterTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CounterTransactions] ([Id]),
        CONSTRAINT [FK_Shipments_ShippingZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ShippingZones] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE TABLE [TransactionLines] (
        [Id] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [Code] varchar(40) NOT NULL,
        [ShippingLevel] int NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
        [ModifiedAtUtc] datetime2 NULL,
        [TransactionId] uniqueidentifier NULL,
        [Quantity] int NOT NULL,
        [UnitPriceAmount] decimal(18,2) NOT NULL,
        [UnitPriceCurrency] varchar(3) NOT NULL,
        CONSTRAINT [PK_TransactionLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Line_Price] CHECK ([UnitPriceAmount] >= 0),
        CONSTRAINT [CK_Line_Product] CHECK ([Type] <> 0 OR [ProductId] IS NOT NULL),
        CONSTRAINT [CK_Line_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_Line_Type] CHECK ([Type] IN (0,1,2)),
        CONSTRAINT [FK_TransactionLines_CounterTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TransactionLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_Code] ON [Branches] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Branches_RegionId] ON [Branches] ([RegionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BranchStock_BranchId_ProductId] ON [BranchStock] ([BranchId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BranchStock_ProductId] ON [BranchStock] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Counters_BranchId_Number] ON [Counters] ([BranchId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CounterTransactions_BranchId] ON [CounterTransactions] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CounterTransactions_CounterId_BranchId] ON [CounterTransactions] ([CounterId], [BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CounterTransactions_StaffId] ON [CounterTransactions] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MoneyOrders_BranchId] ON [MoneyOrders] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MoneyOrders_IdentityVerificationId] ON [MoneyOrders] ([IdentityVerificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MoneyOrders_StaffId] ON [MoneyOrders] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_TransactionId] ON [Payments] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductCategories_Name] ON [ProductCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Regions_Code] ON [Regions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shipments_TransactionId] ON [Shipments] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shipments_ZoneId] ON [Shipments] ([ZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingRates_ZoneId_ServiceLevel] ON [ShippingRates] ([ZoneId], [ServiceLevel]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingZones_Code] ON [ShippingZones] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Staff_BranchId] ON [Staff] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Staff_Email] ON [Staff] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Staff_Role] ON [Staff] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_BranchId] ON [StockAdjustments] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_ProductId] ON [StockAdjustments] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TransactionLines_ProductId] ON [TransactionLines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TransactionLines_TransactionId] ON [TransactionLines] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030123939_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030123939_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030124045_AddStoredProcedures'
)
BEGIN
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales @BranchCode varchar(40), @Date date
    AS
    BEGIN
     SET NOCOUNT ON;
     IF @Date IS NULL OR NULLIF(LTRIM(RTRIM(@BranchCode)), '''') IS NULL THROW 50001, ''Branch code and date are required.'', 1;
     DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);
     IF @BranchId IS NULL THROW 50002, ''Branch was not found.'', 1;
     SELECT t.BranchId, CAST(t.OccurredAt AS date) AS [Day],
     SUM(CASE WHEN l.Type = 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Retail,
     SUM(CASE WHEN l.Type <> 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Services,
     l.UnitPriceCurrency AS Currency
     FROM dbo.CounterTransactions t INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id
     WHERE t.BranchId=@BranchId AND t.Status=1 AND t.OccurredAt>=@Date AND t.OccurredAt<DATEADD(day,1,@Date)
     GROUP BY t.BranchId, CAST(t.OccurredAt AS date), l.UnitPriceCurrency;
    END;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030124045_AddStoredProcedures'
)
BEGIN
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock @BranchCode varchar(40), @Sku varchar(40), @Delta int, @Reason nvarchar(500), @ChangedBy nvarchar(100)
    AS
    BEGIN
     SET NOCOUNT ON;
     SET XACT_ABORT ON;
     IF @Delta IS NULL OR @Delta=0 THROW 50004,''Stock adjustment must be nonzero.'',1;
     IF NULLIF(LTRIM(RTRIM(@Reason)),'''') IS NULL OR NULLIF(LTRIM(RTRIM(@ChangedBy)),'''') IS NULL THROW 50005,''Reason and actor are required.'',1;
     DECLARE @BranchId uniqueidentifier=(SELECT Id FROM dbo.Branches WHERE Code=@BranchCode);
     DECLARE @ProductId uniqueidentifier=(SELECT Id FROM dbo.Products WHERE Sku=@Sku);
     IF @BranchId IS NULL OR @ProductId IS NULL THROW 50006,''Branch or SKU was not found.'',1;
     DECLARE @OwnTransaction bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
     BEGIN TRY
      IF @OwnTransaction=1 BEGIN TRANSACTION; ELSE SAVE TRANSACTION AdjustStockSavepoint;
      UPDATE dbo.BranchStock SET Quantity=Quantity+@Delta,ModifiedAtUtc=SYSUTCDATETIME()
       WHERE BranchId=@BranchId AND ProductId=@ProductId AND CAST(Quantity AS bigint)+@Delta BETWEEN 0 AND 1000000;
      IF @@ROWCOUNT<>1 THROW 50007,''Stock is missing or adjustment exceeds permitted quantity.'',1;
      INSERT dbo.StockAdjustments(Id,BranchId,ProductId,Delta,Reason,ChangedBy,CreatedAtUtc,CreatedBy)
       VALUES(NEWID(),@BranchId,@ProductId,@Delta,@Reason,@ChangedBy,SYSUTCDATETIME(),@ChangedBy);
      IF @OwnTransaction=1 COMMIT TRANSACTION;
     END TRY
     BEGIN CATCH
      IF @OwnTransaction=1 AND XACT_STATE()<>0 ROLLBACK TRANSACTION;
      ELSE IF @OwnTransaction=0 AND XACT_STATE()=1 ROLLBACK TRANSACTION AdjustStockSavepoint;
      THROW;
     END CATCH
    END;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030124045_AddStoredProcedures'
)
BEGIN
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport @BranchCode varchar(40)=NULL, @Threshold int=3
    AS
    BEGIN
     SET NOCOUNT ON;
     IF @Threshold IS NULL OR @Threshold<0 THROW 50003, ''Threshold must be nonnegative.'', 1;
     IF @BranchCode IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Branches WHERE Code=@BranchCode) THROW 50002,''Branch was not found.'',1;
     SELECT s.BranchId,s.ProductId,p.Name AS Product,s.Quantity
     FROM dbo.BranchStock s INNER JOIN dbo.Branches b ON b.Id=s.BranchId INNER JOIN dbo.Products p ON p.Id=s.ProductId
     WHERE (@BranchCode IS NULL OR b.Code=@BranchCode) AND s.Quantity<=@Threshold
     ORDER BY s.Quantity,s.BranchId,s.ProductId OPTION(RECOMPILE);
    END;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030124045_AddStoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030124045_AddStoredProcedures', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030125213_AddMeasuredWorkloadIndexes'
)
BEGIN
    CREATE INDEX IX_Stock_Branch_Quantity ON dbo.BranchStock(BranchId,Quantity) INCLUDE(ProductId);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030125213_AddMeasuredWorkloadIndexes'
)
BEGIN
    CREATE INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions(BranchId,OccurredAt) INCLUDE(Status);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030125213_AddMeasuredWorkloadIndexes'
)
BEGIN
    CREATE INDEX IX_Shipment_Zone_Level ON dbo.Shipments(ZoneId,ServiceLevel) INCLUDE(TransactionId,Weight);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030125213_AddMeasuredWorkloadIndexes'
)
BEGIN
    CREATE INDEX IX_MoneyOrder_Branch_Date ON dbo.MoneyOrders(BranchId,OccurredAtUtc) INCLUDE(OrderAmount,IdentityVerificationId);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030125213_AddMeasuredWorkloadIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030125213_AddMeasuredWorkloadIndexes', N'10.0.13');
END;

COMMIT;
GO

