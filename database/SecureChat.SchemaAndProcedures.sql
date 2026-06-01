/*
    SecureChat SQL Server schema and stored procedures.
    This script mirrors the current EF Core model and adds procedural entry points
    for authentication, OTP, device, token, security logging, friends, and messages.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'SecureChat') IS NULL
BEGIN
    CREATE DATABASE [SecureChat];
END
GO

USE [SecureChat];
GO

CREATE TABLE [dbo].[OtpRequests] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_OtpRequests_Id] DEFAULT newsequentialid(),
    [Email] nvarchar(256) NOT NULL,
    [OtpCode] nvarchar(10) NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsUsed] bit NOT NULL CONSTRAINT [DF_OtpRequests_IsUsed] DEFAULT 0,
    [Attempts] int NOT NULL CONSTRAINT [DF_OtpRequests_Attempts] DEFAULT 0,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_OtpRequests_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_OtpRequests] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[SecurityLogs] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_SecurityLogs_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NULL,
    [DeviceId] uniqueidentifier NULL,
    [EventType] nvarchar(100) NOT NULL,
    [IpAddress] nvarchar(100) NOT NULL CONSTRAINT [DF_SecurityLogs_IpAddress] DEFAULT N'',
    [UserAgent] nvarchar(500) NOT NULL CONSTRAINT [DF_SecurityLogs_UserAgent] DEFAULT N'',
    [Details] nvarchar(max) NOT NULL CONSTRAINT [DF_SecurityLogs_Details] DEFAULT N'',
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_SecurityLogs_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_SecurityLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[Users] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Users_Id] DEFAULT newsequentialid(),
    [Email] nvarchar(256) NOT NULL,
    [DisplayName] nvarchar(100) NOT NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [IsOnline] bit NOT NULL CONSTRAINT [DF_Users_IsOnline] DEFAULT 0,
    [LastSeen] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Users_UpdatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[Devices] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Devices_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NOT NULL,
    [DeviceName] nvarchar(200) NOT NULL,
    [Platform] nvarchar(50) NOT NULL,
    [DeviceFingerprint] nvarchar(128) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [LastLoginAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Devices_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Devices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Devices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[PublicKeys] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PublicKeys_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NOT NULL,
    [DeviceId] uniqueidentifier NOT NULL,
    [PublicKeyPem] nvarchar(max) NOT NULL,
    [KeyFingerprint] nvarchar(128) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_PublicKeys_IsActive] DEFAULT 1,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_PublicKeys_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_PublicKeys] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PublicKeys_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices]([Id]),
    CONSTRAINT [FK_PublicKeys_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[RefreshTokens] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_RefreshTokens_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NOT NULL,
    [DeviceId] uniqueidentifier NOT NULL,
    [Token] nvarchar(500) NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsRevoked] bit NOT NULL CONSTRAINT [DF_RefreshTokens_IsRevoked] DEFAULT 0,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [RevokedAt] datetimeoffset NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices]([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[BlockedUsers] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_BlockedUsers_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NOT NULL,
    [BlockedUserId] uniqueidentifier NOT NULL,
    [BlockedAt] datetimeoffset NOT NULL CONSTRAINT [DF_BlockedUsers_BlockedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_BlockedUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BlockedUsers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_BlockedUsers_Users_BlockedUserId] FOREIGN KEY ([BlockedUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[Friends] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Friends_Id] DEFAULT newsequentialid(),
    [UserId] uniqueidentifier NOT NULL,
    [FriendUserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Friends_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Friends] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Friends_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Friends_Users_FriendUserId] FOREIGN KEY ([FriendUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[FriendRequests] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_FriendRequests_Id] DEFAULT newsequentialid(),
    [FromUserId] uniqueidentifier NOT NULL,
    [ToUserId] uniqueidentifier NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [SentAt] datetimeoffset NOT NULL CONSTRAINT [DF_FriendRequests_SentAt] DEFAULT SYSUTCDATETIME(),
    [RespondedAt] datetimeoffset NULL,
    CONSTRAINT [PK_FriendRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FriendRequests_Users_FromUserId] FOREIGN KEY ([FromUserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_FriendRequests_Users_ToUserId] FOREIGN KEY ([ToUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[Groups] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Groups_Id] DEFAULT newsequentialid(),
    [Name] nvarchar(100) NOT NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [CreatorId] uniqueidentifier NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Groups_CreatedAt] DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Groups_UpdatedAt] DEFAULT SYSUTCDATETIME(),
    [IsActive] bit NOT NULL CONSTRAINT [DF_Groups_IsActive] DEFAULT 1,
    CONSTRAINT [PK_Groups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Groups_Users_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[Chats] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Chats_Id] DEFAULT newsequentialid(),
    [Type] nvarchar(50) NOT NULL,
    [GroupId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Chats_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Chats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Chats_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Groups]([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [dbo].[ChatParticipants] (
    [ChatId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ChatParticipants] PRIMARY KEY ([ChatId], [UserId]),
    CONSTRAINT [FK_ChatParticipants_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [dbo].[Chats]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChatParticipants_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[GroupMembers] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_GroupMembers_Id] DEFAULT newsequentialid(),
    [GroupId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [JoinedAt] datetimeoffset NOT NULL CONSTRAINT [DF_GroupMembers_JoinedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_GroupMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupMembers_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Groups]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[GroupKeys] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_GroupKeys_Id] DEFAULT newsequentialid(),
    [GroupId] uniqueidentifier NOT NULL,
    [EncryptedGroupKey] varbinary(max) NOT NULL,
    [ForDeviceId] uniqueidentifier NOT NULL,
    [KeyVersion] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_GroupKeys_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_GroupKeys] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupKeys_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Groups]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupKeys_Devices_ForDeviceId] FOREIGN KEY ([ForDeviceId]) REFERENCES [dbo].[Devices]([Id])
);
GO

CREATE TABLE [dbo].[Messages] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Messages_Id] DEFAULT newsequentialid(),
    [ChatId] uniqueidentifier NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [EncryptedContent] varbinary(max) NOT NULL,
    [MessageType] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [IV] varbinary(max) NOT NULL,
    [AuthTag] varbinary(max) NOT NULL,
    [SentAt] datetimeoffset NOT NULL CONSTRAINT [DF_Messages_SentAt] DEFAULT SYSUTCDATETIME(),
    [DeliveredAt] datetimeoffset NULL,
    [ReadAt] datetimeoffset NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_Messages_IsDeleted] DEFAULT 0,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [dbo].[Chats]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE TABLE [dbo].[Attachments] (
    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Attachments_Id] DEFAULT newsequentialid(),
    [MessageId] uniqueidentifier NULL,
    [FileName] nvarchar(255) NOT NULL,
    [FileSize] bigint NOT NULL,
    [MimeType] nvarchar(100) NOT NULL,
    [EncryptedStoragePath] nvarchar(1000) NOT NULL,
    [EncryptedFileKey] varbinary(max) NOT NULL,
    [IV] varbinary(max) NOT NULL,
    [AuthTag] varbinary(max) NOT NULL,
    [UploadedAt] datetimeoffset NOT NULL CONSTRAINT [DF_Attachments_UploadedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attachments_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [dbo].[Messages]([Id]) ON DELETE SET NULL
);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
CREATE INDEX [IX_OtpRequests_Email] ON [dbo].[OtpRequests]([Email]);
CREATE UNIQUE INDEX [IX_Devices_DeviceFingerprint] ON [dbo].[Devices]([DeviceFingerprint]);
CREATE INDEX [IX_Devices_UserId] ON [dbo].[Devices]([UserId]);
CREATE UNIQUE INDEX [IX_PublicKeys_DeviceId] ON [dbo].[PublicKeys]([DeviceId]);
CREATE INDEX [IX_PublicKeys_UserId_DeviceId_IsActive] ON [dbo].[PublicKeys]([UserId], [DeviceId], [IsActive]);
CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens]([Token]);
CREATE INDEX [IX_RefreshTokens_UserId_DeviceId_IsRevoked] ON [dbo].[RefreshTokens]([UserId], [DeviceId], [IsRevoked]);
CREATE UNIQUE INDEX [IX_Friends_UserId_FriendUserId] ON [dbo].[Friends]([UserId], [FriendUserId]);
CREATE UNIQUE INDEX [IX_BlockedUsers_UserId_BlockedUserId] ON [dbo].[BlockedUsers]([UserId], [BlockedUserId]);
CREATE INDEX [IX_SecurityLogs_UserId_CreatedAt] ON [dbo].[SecurityLogs]([UserId], [CreatedAt]);
CREATE INDEX [IX_ChatParticipants_UserId] ON [dbo].[ChatParticipants]([UserId]);
CREATE INDEX [IX_Messages_ChatId_SentAt] ON [dbo].[Messages]([ChatId], [SentAt]);
CREATE INDEX [IX_Attachments_MessageId] ON [dbo].[Attachments]([MessageId]);
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_LogSecurityEvent]
    @UserId uniqueidentifier = NULL,
    @DeviceId uniqueidentifier = NULL,
    @EventType nvarchar(100),
    @IpAddress nvarchar(100) = N'',
    @UserAgent nvarchar(500) = N'',
    @Details nvarchar(max) = N''
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[SecurityLogs] ([UserId], [DeviceId], [EventType], [IpAddress], [UserAgent], [Details])
    VALUES (@UserId, @DeviceId, @EventType, @IpAddress, @UserAgent, @Details);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_CreateOtpRequest]
    @Email nvarchar(256),
    @OtpCode nvarchar(10),
    @ExpiresAt datetimeoffset
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[OtpRequests] ([Email], [OtpCode], [ExpiresAt])
    OUTPUT inserted.*
    VALUES (LOWER(@Email), @OtpCode, @ExpiresAt);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetValidOtpRequest]
    @Email nvarchar(256),
    @OtpCode nvarchar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) *
    FROM [dbo].[OtpRequests]
    WHERE LOWER([Email]) = LOWER(@Email)
      AND [OtpCode] = @OtpCode
      AND [IsUsed] = 0
      AND [ExpiresAt] > SYSUTCDATETIME()
    ORDER BY [CreatedAt] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_MarkOtpUsed]
    @OtpRequestId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[OtpRequests]
    SET [IsUsed] = 1
    WHERE [Id] = @OtpRequestId;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_IncrementOtpAttempts]
    @OtpRequestId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[OtpRequests]
    SET [Attempts] = [Attempts] + 1
    WHERE [Id] = @OtpRequestId;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_CreateUser]
    @Email nvarchar(256),
    @DisplayName nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Users] ([Email], [DisplayName])
    OUTPUT inserted.*
    VALUES (LOWER(@Email), @DisplayName);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterDevice]
    @UserId uniqueidentifier,
    @DeviceName nvarchar(200),
    @Platform nvarchar(50),
    @DeviceFingerprint nvarchar(128),
    @PublicKeyPem nvarchar(max),
    @KeyFingerprint nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        DECLARE @DeviceId uniqueidentifier = NEWID();

        INSERT INTO [dbo].[Devices] ([Id], [UserId], [DeviceName], [Platform], [DeviceFingerprint], [Status], [LastLoginAt])
        VALUES (@DeviceId, @UserId, @DeviceName, @Platform, @DeviceFingerprint, N'Active', SYSUTCDATETIME());

        INSERT INTO [dbo].[PublicKeys] ([UserId], [DeviceId], [PublicKeyPem], [KeyFingerprint], [IsActive])
        VALUES (@UserId, @DeviceId, @PublicKeyPem, @KeyFingerprint, 1);

        EXEC [dbo].[sp_LogSecurityEvent] @UserId, @DeviceId, N'DeviceRegistered', N'', N'', @DeviceName;

        SELECT * FROM [dbo].[Devices] WHERE [Id] = @DeviceId;
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_CreateRefreshToken]
    @UserId uniqueidentifier,
    @DeviceId uniqueidentifier,
    @Token nvarchar(500),
    @ExpiresAt datetimeoffset
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[RefreshTokens] ([UserId], [DeviceId], [Token], [ExpiresAt])
    OUTPUT inserted.*
    VALUES (@UserId, @DeviceId, @Token, @ExpiresAt);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RevokeRefreshTokensForDevice]
    @DeviceId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[RefreshTokens]
    SET [IsRevoked] = 1, [RevokedAt] = SYSUTCDATETIME()
    WHERE [DeviceId] = @DeviceId AND [IsRevoked] = 0;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RevokeDevice]
    @UserId uniqueidentifier,
    @DeviceId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        UPDATE [dbo].[Devices]
        SET [Status] = N'Revoked'
        WHERE [Id] = @DeviceId AND [UserId] = @UserId;

        UPDATE [dbo].[PublicKeys]
        SET [IsActive] = 0
        WHERE [DeviceId] = @DeviceId AND [UserId] = @UserId;

        EXEC [dbo].[sp_RevokeRefreshTokensForDevice] @DeviceId;
        EXEC [dbo].[sp_LogSecurityEvent] @UserId, @DeviceId, N'DeviceRevoked', N'', N'', N'Device revoked';
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SendFriendRequest]
    @FromUserId uniqueidentifier,
    @ToUserId uniqueidentifier,
    @Message nvarchar(500) = N''
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[FriendRequests] ([FromUserId], [ToUserId], [Message], [Status])
    OUTPUT inserted.*
    VALUES (@FromUserId, @ToUserId, @Message, N'Pending');
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AcceptFriendRequest]
    @RequestId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        DECLARE @FromUserId uniqueidentifier;
        DECLARE @ToUserId uniqueidentifier;

        SELECT @FromUserId = [FromUserId], @ToUserId = [ToUserId]
        FROM [dbo].[FriendRequests]
        WHERE [Id] = @RequestId AND [Status] = N'Pending';

        IF @FromUserId IS NULL
            THROW 51000, 'Friend request not found or already processed.', 1;

        UPDATE [dbo].[FriendRequests]
        SET [Status] = N'Accepted', [RespondedAt] = SYSUTCDATETIME()
        WHERE [Id] = @RequestId;

        INSERT INTO [dbo].[Friends] ([UserId], [FriendUserId])
        SELECT @FromUserId, @ToUserId
        WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Friends] WHERE [UserId] = @FromUserId AND [FriendUserId] = @ToUserId);

        INSERT INTO [dbo].[Friends] ([UserId], [FriendUserId])
        SELECT @ToUserId, @FromUserId
        WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Friends] WHERE [UserId] = @ToUserId AND [FriendUserId] = @FromUserId);
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_CreateDirectChat]
    @UserId uniqueidentifier,
    @OtherUserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        DECLARE @ChatId uniqueidentifier = NEWID();
        INSERT INTO [dbo].[Chats] ([Id], [Type]) VALUES (@ChatId, N'Direct');
        INSERT INTO [dbo].[ChatParticipants] ([ChatId], [UserId]) VALUES (@ChatId, @UserId), (@ChatId, @OtherUserId);
        SELECT * FROM [dbo].[Chats] WHERE [Id] = @ChatId;
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SendMessage]
    @ChatId uniqueidentifier,
    @SenderId uniqueidentifier,
    @EncryptedContent varbinary(max),
    @MessageType nvarchar(50),
    @IV varbinary(max),
    @AuthTag varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Messages] ([ChatId], [SenderId], [EncryptedContent], [MessageType], [Status], [IV], [AuthTag])
    OUTPUT inserted.*
    VALUES (@ChatId, @SenderId, @EncryptedContent, @MessageType, N'Sending', @IV, @AuthTag);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_MarkMessageRead]
    @MessageId uniqueidentifier,
    @UserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Messages]
    SET [Status] = N'Read', [ReadAt] = SYSUTCDATETIME()
    WHERE [Id] = @MessageId
      AND EXISTS (
          SELECT 1 FROM [dbo].[ChatParticipants]
          WHERE [ChatId] = [Messages].[ChatId] AND [UserId] = @UserId
      );
END;
GO
