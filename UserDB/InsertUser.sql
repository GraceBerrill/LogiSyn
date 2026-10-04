IF COL_LENGTH('dbo.[User]', 'MongoId') IS NULL
BEGIN
    ALTER TABLE [dbo].[User]
    ADD [MongoId] NVARCHAR(24) NULL;
END;
GO

DECLARE @AdminPassword NVARCHAR(100) =
    'PBKDF2-SHA256$600000$PX/eF/pqtB/py1Ge9f969A==$JCjN8neakDQb67c+x298kIPh97IN+QYn994VVpiUo9I=';

IF EXISTS (
    SELECT 1
    FROM [dbo].[User]
    WHERE [Username] = 'admin'
)
BEGIN
    UPDATE [dbo].[User]
    SET [Password] = @AdminPassword,
        [Role] = 'Admin'
    WHERE [Username] = 'admin';
END
ELSE(localdb)\MSSQLLocalDB
BEGIN
    INSERT INTO [dbo].[User] ([Username], [Password], [Role])
    VALUES ('admin', @AdminPassword, 'Admin');
END;
GO
