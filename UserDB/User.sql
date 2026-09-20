CREATE TABLE [dbo].[User]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Username] NCHAR(10) NULL, 
    [Password] NCHAR(10) NULL, 
    [Role] NCHAR(10) NULL
)