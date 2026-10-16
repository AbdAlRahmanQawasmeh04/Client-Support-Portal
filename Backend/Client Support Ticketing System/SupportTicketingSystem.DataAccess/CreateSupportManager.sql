USE [ClientSupportTicketingSystemDb];
GO

DECLARE @Username NVARCHAR(450) = N'manager';
DECLARE @Email NVARCHAR(450) = N'manager@example.com';

DECLARE @PasswordHash NVARCHAR(MAX) =
    N'AQAAAAEAACcQAAAAEF5tKaBgN5oDGtZy7+Lc6RMAyVHew0f74QzXRNw9rfwX1+jueVdqzxE7AI1uuubjqQ==';

IF EXISTS
(
    SELECT 1
    FROM dbo.Users
    WHERE Username = @Username
      AND Email = @Email
      AND Role = 3
)
BEGIN
    PRINT N'Support manager already exists.';
END
ELSE
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE Username = @Username
           OR Email = @Email
    )
    BEGIN
        THROW 50001,
            'The manager username or email is used by another account.',
            1;
    END;

    INSERT INTO dbo.Users
    (
        FullName,
        Username,
        MobileNumber,
        Email,
        PasswordHash,
        ImagePath,
        DateOfBirth,
        Address,
        IsActive,
        Role
    )
    VALUES
    (
        N'Support Manager',
        @Username,
        N'0790000000',
        @Email,
        @PasswordHash,
        NULL,
        '19900101',
        N'Amman',
        1,
        3
    );

    PRINT N'Support manager created successfully.';
END;
GO

SELECT
    Id,
    FullName,
    Username,
    Email,
    IsActive,
    Role
FROM dbo.Users
WHERE Username = N'manager';