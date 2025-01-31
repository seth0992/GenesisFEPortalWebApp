-- Crear un procedimiento almacenado para actualizar secretos (opcional pero útil)
CREATE OR ALTER PROCEDURE [Security].[UpdateSecret]
    @TenantId BIGINT,
    @Key NVARCHAR(100),
    @Value NVARCHAR(MAX),
    @Description NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE [Security].[Secrets] AS target
    USING (SELECT @TenantId, @Key, @Value, @Description) 
        AS source (TenantId, [Key], [Value], [Description])
    ON target.TenantId = source.TenantId 
        AND target.[Key] = source.[Key]
    WHEN MATCHED THEN
        UPDATE SET 
            [Value] = source.[Value],
            [Description] = source.[Description],
            [UpdatedAt] = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (TenantId, [Key], [Value], [Description], IsActive, CreatedAt)
        VALUES (
            source.TenantId, 
            source.[Key], 
            source.[Value], 
            source.[Description], 
            1, 
            GETUTCDATE()
        );
END
GO

-- Agregar permisos necesarios (ajustar según tus necesidades)
GRANT SELECT, INSERT, UPDATE ON [Security].[Secrets] TO [YourApplicationRole]
GO

GRANT EXECUTE ON [Security].[UpdateSecret] TO [YourApplicationRole]
GO