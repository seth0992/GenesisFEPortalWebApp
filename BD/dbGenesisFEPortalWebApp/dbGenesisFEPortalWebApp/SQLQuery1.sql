select * from core.Tenants
select * from Security.Users
select * from Security.Secrets

select * from core.Customers


SELECT * FROM Security.Secrets 
WHERE TenantId = 4 AND [Key] = 'JWT_SECRET';

UPDATE Security.Secrets
SET EncryptedValue = 'MySuperSecret12k3jioasd8o12k3joiajsdij1l2kj3!!!!1k;lajskdjalkdj1sdlkj1ndas123qq',
    IsEncrypted = 0  -- Temporalmente lo guardamos sin encriptar
WHERE TenantId = 5 
AND [Key] = 'JWT_SECRET';

-- Verificar y/o crear el secreto JWT para el tenant específico
DECLARE @TenantId BIGINT = 5; -- El tenant ID que necesitamos

IF NOT EXISTS (
    SELECT 1 FROM Security.Secrets 
    WHERE TenantId = @TenantId 
    AND [Key] = 'JWT_SECRET'
)
BEGIN
    INSERT INTO Security.Secrets (
        TenantId,
        [Key],
        EncryptedValue,
        Description,
        IsEncrypted,
        IsActive,
        CreatedAt
    )
    VALUES (
        @TenantId,
        'JWT_SECRET',
        'MySuperSecret12k3jioasd8o12k3joiajsdij1l2kj3!!!!1k;lajskdjalkdj1sdlkj1ndas123qq',
        'JWT signing key for authentication',
        0,
        1,
        GETUTCDATE()
    );
    
    PRINT 'Secreto JWT creado para tenant ' + CAST(@TenantId AS VARCHAR);
END
ELSE
BEGIN
    UPDATE Security.Secrets
    SET EncryptedValue = 'MySuperSecret12k3jioasd8o12k3joiajsdij1l2kj3!!!!1k;lajskdjalkdj1sdlkj1ndas123qq',
        IsEncrypted = 0,
        UpdatedAt = GETUTCDATE()
    WHERE TenantId = @TenantId 
    AND [Key] = 'JWT_SECRET';
    
    PRINT 'Secreto JWT actualizado para tenant ' + CAST(@TenantId AS VARCHAR);
END