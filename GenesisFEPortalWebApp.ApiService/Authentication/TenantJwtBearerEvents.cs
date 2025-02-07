using GenesisFEPortalWebApp.BL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GenesisFEPortalWebApp.ApiService.Authentication
{
     /// <summary>
      /// Clase que maneja los eventos de autenticación JWT específicos para cada tenant.
      /// Esta clase extiende JwtBearerEvents para personalizar el proceso de validación
      /// de tokens JWT, permitiendo usar diferentes claves de firma para diferentes tenants.
      /// </summary>
        public class TenantJwtBearerEvents : JwtBearerEvents
        {
            private readonly ISecretService _secretService;
            private readonly IConfiguration _configuration;
            private readonly ILogger<TenantJwtBearerEvents> _logger;

            public TenantJwtBearerEvents(
                ISecretService secretService,
                IConfiguration configuration,
                ILogger<TenantJwtBearerEvents> logger)
            {
                _secretService = secretService;
                _configuration = configuration;
                _logger = logger;
            }

            /// <summary>
            /// Se ejecuta cuando un token JWT está siendo validado.
            /// Este método personaliza la validación para usar la clave específica del tenant.
            /// </summary>
            public override async Task TokenValidated(TokenValidatedContext context)
            {
                try
                {
                    // Extraer el TenantId del token JWT
                    var tenantIdClaim = context.Principal?.Claims
                        .FirstOrDefault(c => c.Type == "TenantId");

                    if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
                    {
                        _logger.LogError("TenantId no encontrado en el token");
                        context.Fail("Token inválido: TenantId no encontrado");
                        return;
                    }

                    _logger.LogInformation("Validando token para tenant {TenantId}", tenantId);

                    // Obtener el secreto específico del tenant desde la base de datos
                    var jwtSecret = await _secretService.GetSecretValueAsync("JWT_SECRET", tenantId);

                    if (string.IsNullOrEmpty(jwtSecret))
                    {
                        _logger.LogError("No se encontró secreto JWT para tenant {TenantId}", tenantId);
                        context.Fail("Configuración de seguridad inválida");
                        return;
                    }

                    // Crear la clave de seguridad con el secreto específico del tenant
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = _configuration["JWT:ValidIssuer"],
                        ValidAudience = _configuration["JWT:ValidAudience"],
                        ClockSkew = TimeSpan.Zero
                    };

                    try
                    {
                        // Obtener el token del contexto
                        var token = context.SecurityToken as JwtSecurityToken;
                        if (token == null)
                        {
                            _logger.LogError("Token no es un JWT válido");
                            context.Fail("Token inválido");
                            return;
                        }

                        // Convertir el token a string para revalidación
                        var tokenString = tokenHandler.WriteToken(token);

                        // Validar el token con el secreto específico del tenant
                        var principal = tokenHandler.ValidateToken(
                            tokenString,
                            validationParameters,
                            out var validatedToken);

                        // Actualizar el contexto con el principal validado
                        context.Principal = principal;

                        _logger.LogInformation(
                            "Token validado exitosamente para tenant {TenantId}",
                            tenantId);
                    }
                    catch (SecurityTokenValidationException ex)
                    {
                        _logger.LogError(ex,
                            "Error validando token para tenant {TenantId}",
                            tenantId);
                        context.Fail(ex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en proceso de validación de token");
                    context.Fail(ex);
                }
            }
        }
    
}
