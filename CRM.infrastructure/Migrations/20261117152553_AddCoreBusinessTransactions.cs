using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations
{
    public partial class AddCoreBusinessTransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous desktop form used manually created tables. Adopt those tables
            // without dropping records, timestamp columns, or their existing defaults.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[JobOrders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobOrders] (
        [JobOrderId] int IDENTITY NOT NULL CONSTRAINT [PK_JobOrders] PRIMARY KEY,
        [EstimateId] nvarchar(200) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StartedAt] datetime2 NOT NULL);
END;
IF OBJECT_ID(N'[dbo].[JobTasks]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTasks] (
        [JobTaskId] int IDENTITY NOT NULL CONSTRAINT [PK_JobTasks] PRIMARY KEY,
        [JobOrderId] int NOT NULL,
        [ServiceId] nvarchar(200) NOT NULL,
        [Remarks] nvarchar(2000) NOT NULL,
        [Status] nvarchar(30) NOT NULL);
END;
IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [PaymentId] int IDENTITY NOT NULL CONSTRAINT [PK_Payments] PRIMARY KEY,
        [JobOrderId] int NOT NULL,
        [PaymentType] nvarchar(30) NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL);
END;
-- Only widen the known legacy text fields; never truncate existing values.
IF COL_LENGTH(N'dbo.JobOrders', N'EstimateId') BETWEEN 1 AND 399
    ALTER TABLE [dbo].[JobOrders] ALTER COLUMN [EstimateId] nvarchar(200) NOT NULL;
IF COL_LENGTH(N'dbo.JobTasks', N'ServiceId') BETWEEN 1 AND 399
    ALTER TABLE [dbo].[JobTasks] ALTER COLUMN [ServiceId] nvarchar(200) NOT NULL;
IF COL_LENGTH(N'dbo.JobTasks', N'Remarks') BETWEEN 1 AND 3999
    ALTER TABLE [dbo].[JobTasks] ALTER COLUMN [Remarks] nvarchar(2000) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_JobTasks_JobOrders_JobOrderId' AND parent_object_id = OBJECT_ID(N'dbo.JobTasks'))
    ALTER TABLE [dbo].[JobTasks] WITH CHECK ADD CONSTRAINT [FK_JobTasks_JobOrders_JobOrderId]
        FOREIGN KEY ([JobOrderId]) REFERENCES [dbo].[JobOrders] ([JobOrderId]) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payments_JobOrders_JobOrderId' AND parent_object_id = OBJECT_ID(N'dbo.Payments'))
    ALTER TABLE [dbo].[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_JobOrders_JobOrderId]
        FOREIGN KEY ([JobOrderId]) REFERENCES [dbo].[JobOrders] ([JobOrderId]) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobTasks_JobOrderId' AND object_id = OBJECT_ID(N'dbo.JobTasks'))
    CREATE INDEX [IX_JobTasks_JobOrderId] ON [dbo].[JobTasks] ([JobOrderId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_JobOrderId' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX [IX_Payments_JobOrderId] ON [dbo].[Payments] ([JobOrderId]);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException(
                "This migration adopts existing business tables. Automatic rollback is disabled to preserve their data; use a reviewed backup/restore procedure.");
        }
    }
}
