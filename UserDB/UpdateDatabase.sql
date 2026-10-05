-- Ensure the [User] table exists and create a compatibility view [Users]
-- Run this script against your LogiSynDb (localdb or server) to update the schema.

IF OBJECT_ID('dbo.[User]', 'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[User]
	(
		[Id]        INT NOT NULL PRIMARY KEY IDENTITY,
		[Username]  NVARCHAR(50)  NOT NULL,
		[Password]  NVARCHAR(256) NOT NULL,
		[Role]      NVARCHAR(50)  NOT NULL,
		[MongoId]   NVARCHAR(24)  NULL,
		[DateAdded] DATETIME NOT NULL DEFAULT GETDATE()
	);
END

IF OBJECT_ID('dbo.[Users]', 'U') IS NULL AND OBJECT_ID('dbo.[Users]', 'V') IS NULL
BEGIN
	EXEC('CREATE VIEW [dbo].[Users] AS SELECT [Id], [Username], [Role], [MongoId], [DateAdded] FROM [dbo].[User]');
END

