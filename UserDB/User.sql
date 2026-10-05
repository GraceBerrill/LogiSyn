CREATE TABLE [dbo].[User]
(
    [Id]        INT NOT NULL PRIMARY KEY IDENTITY,
    [Username]  NVARCHAR(50)  NOT NULL,
    [Password]  NVARCHAR(256) NOT NULL,
    [Role]      NVARCHAR(50)  NOT NULL,
    [MongoId]   NVARCHAR(24)  NULL,
    [DateAdded] DATETIME NOT NULL DEFAULT GETDATE()
);

-- Unique index on Username for login lookups and uniqueness enforcement
CREATE UNIQUE INDEX [UX_User_Username] ON [dbo].[User] ([Username]);
