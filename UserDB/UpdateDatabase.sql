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

-- If there is no [Users] object (table or view), create a view named [Users]
-- that selects from the canonical [User] table. This preserves compatibility
-- with code that still queries [Users].
IF OBJECT_ID('dbo.[Users]', 'U') IS NULL AND OBJECT_ID('dbo.[Users]', 'V') IS NULL
BEGIN
	EXEC('CREATE VIEW [dbo].[Users] AS SELECT * FROM [dbo].[User]');
END

-- Optional: you can verify by selecting top rows
-- SELECT TOP (10) * FROM [dbo].[User];
-- SELECT TOP (10) * FROM [dbo].[Users];
