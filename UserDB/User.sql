CREATE TABLE [dbo].[User]
(
    [Id]        INT NOT NULL PRIMARY KEY IDENTITY,
    [Username]  NVARCHAR(50)  NOT NULL,
    [Password]  NVARCHAR(100) NOT NULL,
    [Role]      NVARCHAR(50)  NOT NULL,
    [MongoId]   NVARCHAR(24)  NULL,
    [DateAdded] DATETIME NOT NULL DEFAULT GETDATE()
);
