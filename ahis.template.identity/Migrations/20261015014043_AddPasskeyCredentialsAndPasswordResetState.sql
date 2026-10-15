BEGIN TRANSACTION;
GO

ALTER TABLE [IdentityUsers] ADD [MustResetPassword] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [PasskeyCeremonies] (
    [Id] uniqueidentifier NOT NULL,
    [ChallengeReferenceHash] binary(32) NOT NULL,
    [ChallengeHash] binary(32) NOT NULL,
    [Purpose] int NOT NULL,
    [UserId] nvarchar(450) NULL,
    [SecurityVersion] nvarchar(64) NULL,
    [RpConfigurationId] nvarchar(100) NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ConsumedAtUtc] datetime2 NULL,
    [RateLimitKeyHash] nvarchar(64) NOT NULL,
    [UserRateLimitKeyHash] nvarchar(64) NULL,
    [OptionsJson] nvarchar(max) NOT NULL,
    [ProtectedChallenge] varbinary(max) NOT NULL,
    CONSTRAINT [PK_PasskeyCeremonies] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PasskeyCeremonies_Purpose] CHECK ([Purpose] IN (1, 2)),
    CONSTRAINT [FK_PasskeyCeremonies_IdentityUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [IdentityUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PasskeyCredentials] (
    [Id] uniqueidentifier NOT NULL,
    [CredentialId] nvarchar(1024) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [PublicKey] varbinary(max) NOT NULL,
    [SignCount] bigint NOT NULL,
    [DisplayName] nvarchar(80) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [LastUsedAtUtc] datetime2 NULL,
    [IsBackupEligible] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsBackedUp] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_PasskeyCredentials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasskeyCredentials_IdentityUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [IdentityUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_PasskeyCeremonies_ChallengeReferenceHash] ON [PasskeyCeremonies] ([ChallengeReferenceHash]);
GO

CREATE INDEX [IX_PasskeyCeremonies_Purpose_UserId_ExpiresAtUtc_ConsumedAtUtc] ON [PasskeyCeremonies] ([Purpose], [UserId], [ExpiresAtUtc], [ConsumedAtUtc]);
GO

CREATE INDEX [IX_PasskeyCeremonies_UserId] ON [PasskeyCeremonies] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_PasskeyCredentials_CredentialId] ON [PasskeyCredentials] ([CredentialId]);
GO

CREATE INDEX [IX_PasskeyCredentials_UserId_CreatedAtUtc] ON [PasskeyCredentials] ([UserId], [CreatedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261015014043_AddPasskeyCredentialsAndPasswordResetState', N'8.0.22');
GO

COMMIT;
GO
