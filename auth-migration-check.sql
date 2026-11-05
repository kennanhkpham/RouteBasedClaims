BEGIN TRANSACTION;
ALTER TABLE [Account] ADD [JobOccupation] nvarchar(100) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261104201911_AddAccountJobOccupation', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NOT NULL AND COL_LENGTH(N'dbo.Account', N'Department') IS NOT NULL BEGIN UPDATE [dbo].[Account] SET [Department] = [JobOccupation] WHERE ([Department] IS NULL OR [Department] = N'') AND [JobOccupation] IS NOT NULL; ALTER TABLE [dbo].[Account] DROP COLUMN [JobOccupation]; END;

IF COL_LENGTH(N'dbo.Account', N'JobOccupation') IS NOT NULL AND COL_LENGTH(N'dbo.Account', N'Department') IS NULL BEGIN EXEC sp_rename N'[dbo].[Account].[JobOccupation]', N'Department', N'COLUMN'; END;

UPDATE [Account] SET [Department] = N'' WHERE [Department] IS NULL;

IF COL_LENGTH(N'dbo.Account', N'Department') IS NOT NULL BEGIN ALTER TABLE [dbo].[Account] ALTER COLUMN [Department] nvarchar(100) NOT NULL; END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105170609_RenameAccountJobOccupationToDepartment', N'10.0.12');

COMMIT;
GO

