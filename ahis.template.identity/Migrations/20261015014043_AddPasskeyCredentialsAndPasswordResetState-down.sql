BEGIN TRANSACTION;
GO


IF EXISTS (SELECT 1 FROM [dbo].[PasskeyCeremonies]) OR EXISTS (SELECT 1 FROM [dbo].[PasskeyCredentials])
    THROW 51000, 'Passkey tables contain rows; rollback is unsafe.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[IdentityUsers] WHERE [MustResetPassword] = 1)
    THROW 51001, 'Password reset interventions are active; rollback is unsafe.', 1;
GO

DROP TABLE [PasskeyCeremonies];
GO

DROP TABLE [PasskeyCredentials];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IdentityUsers]') AND [c].[name] = N'MustResetPassword');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [IdentityUsers] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [IdentityUsers] DROP COLUMN [MustResetPassword];
GO

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261015014043_AddPasskeyCredentialsAndPasswordResetState';
GO

COMMIT;
GO
