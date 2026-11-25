BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE TABLE [AppointmentHistories] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [PreviousScheduledStart] datetime2 NULL,
        [NewScheduledStart] datetime2 NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Actor] nvarchar(150) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AppointmentHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentHistories_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([AppointmentId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE TABLE [JobCancellationRequests] (
        [RequestId] int NOT NULL IDENTITY,
        [RowVersion] rowversion NOT NULL,
        [JobOrderId] int NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [RequestedBy] nvarchar(150) NOT NULL,
        [ReasonCategory] nvarchar(100) NOT NULL,
        [ReasonExplanation] nvarchar(2000) NOT NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT N'Pending Review',
        [TotalPaidAtRequest] decimal(18,2) NOT NULL,
        [IncurredCharges] decimal(18,2) NOT NULL,
        [ProposedRefund] decimal(18,2) NOT NULL,
        [ApprovedRefund] decimal(18,2) NULL,
        [ReviewJustification] nvarchar(2000) NULL,
        [ReviewedBy] nvarchar(150) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JobCancellationRequests] PRIMARY KEY ([RequestId]),
        CONSTRAINT [FK_JobCancellationRequests_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE TABLE [Refunds] (
        [RefundId] int NOT NULL IDENTITY,
        [RowVersion] rowversion NOT NULL,
        [CancellationRequestId] int NULL,
        [JobOrderId] int NOT NULL,
        [PaymentId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [RefundMethod] nvarchar(50) NOT NULL,
        [RefundDateUtc] datetime2 NOT NULL,
        [RecipientName] nvarchar(200) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [AuthorizedBy] nvarchar(150) NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Refunds] PRIMARY KEY ([RefundId]),
        CONSTRAINT [FK_Refunds_JobCancellationRequests_CancellationRequestId] FOREIGN KEY ([CancellationRequestId]) REFERENCES [JobCancellationRequests] ([RequestId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Refunds_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Refunds_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_AppointmentHistories_AppointmentId] ON [AppointmentHistories] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_JobCancellationRequests_JobOrderId] ON [JobCancellationRequests] ([JobOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_JobCancellationRequests_Status] ON [JobCancellationRequests] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_Refunds_CancellationRequestId] ON [Refunds] ([CancellationRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_Refunds_JobOrderId] ON [Refunds] ([JobOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE INDEX [IX_Refunds_PaymentId] ON [Refunds] ([PaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Refunds_SubmissionId] ON [Refunds] ([SubmissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124033501_AddJobCancellationRefundsAndAppointmentHistory', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobTasks] ADD [ArchiveReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobTasks] ADD [ArchivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobTasks] ADD [ArchivedBy] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobTasks] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobOrders] ADD [ArchiveReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobOrders] ADD [ArchivedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobOrders] ADD [ArchivedBy] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    ALTER TABLE [JobOrders] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    CREATE INDEX [IX_JobTasks_IsArchived] ON [JobTasks] ([IsArchived]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    CREATE INDEX [IX_JobOrders_IsArchived] ON [JobOrders] ([IsArchived]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124100231_AddJobOrderAndTaskArchiving'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124100231_AddJobOrderAndTaskArchiving', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [Payments] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [JobOrders] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [Appointments] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE TABLE [Branches] (
        [BranchId] int NOT NULL IDENTITY,
        [BranchCode] nvarchar(50) NOT NULL,
        [BranchName] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ContactNumber] nvarchar(50) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([BranchId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE TABLE [TenantDatabaseIdentity] (
        [Id] int NOT NULL,
        [TenantId] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_TenantDatabaseIdentity] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TenantIdentity_Singleton] CHECK ([Id] = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE INDEX [IX_Payments_BranchId] ON [Payments] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE INDEX [IX_JobOrders_BranchId] ON [JobOrders] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE INDEX [IX_Appointments_BranchId] ON [Appointments] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_BranchCode] ON [Branches] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'BranchCode', N'BranchName', N'Address', N'ContactNumber', N'IsActive', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[Branches]'))
        SET IDENTITY_INSERT [Branches] ON;
    EXEC(N'INSERT INTO [Branches] ([BranchCode], [BranchName], [Address], [ContactNumber], [IsActive], [CreatedAtUtc])
    VALUES (N''MAIN'', N''RevCare Auto Hub - Main Branch'', N''RevCare Central Workshop'', N''+63 2 8123 4567'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'BranchCode', N'BranchName', N'Address', N'ContactNumber', N'IsActive', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[Branches]'))
        SET IDENTITY_INSERT [Branches] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'TenantId') AND [object_id] = OBJECT_ID(N'[TenantDatabaseIdentity]'))
        SET IDENTITY_INSERT [TenantDatabaseIdentity] ON;
    EXEC(N'INSERT INTO [TenantDatabaseIdentity] ([Id], [TenantId])
    VALUES (1, N''tenant-a'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'TenantId') AND [object_id] = OBJECT_ID(N'[TenantDatabaseIdentity]'))
        SET IDENTITY_INSERT [TenantDatabaseIdentity] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    UPDATE [Payments] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    UPDATE [JobOrders] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    UPDATE [Appointments] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [JobOrders] ADD CONSTRAINT [FK_JobOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124131444_AddTenantIdentityAndBranchesToMasterCrm', N'9.0.0');
END;

COMMIT;
GO

