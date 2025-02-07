using Azure.Core;
using Azure;
using GenesisFEPortalWebApp.BL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using System.Text;

namespace GenesisFEPortalWebApp.ApiService.Authentication
{
    /// <summary>
    /// Manejador personalizado de autenticación para sistema multi-tenant.
    /// Gestiona la validación de tokens JWT usando secretos específicos por tenant.
    /// </summary>
    public class MultiTenantAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
    {
        private readonly ISecretRepository _secretRepository;
        private readonly ILogger<MultiTenantAuthenticationHandler> _logger;

        public MultiTenantAuthenticationHandler(
            ISecretRepository secretRepository,
            IOptionsMonitor<JwtBearerOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, loggerFactory, encoder, clock)
        {
            _secretRepository = secretRepository;
            _logger = loggerFactory.CreateLogger<MultiTenantAuthenticationHandler>();
        }

        /// <summary>
        /// Método principal que maneja la autenticación de cada solicitud.
        /// </summary>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // Verificar si existe el header de autorización
                if (!Request.Headers.ContainsKey("Authorization"))
                {
                    _logger.LogInformation("No se encontró header de autorización");
                    return AuthenticateResult.NoResult();
                }

                // Obtener y validar el formato del token
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token no tiene formato Bearer");
                    return AuthenticateResult.NoResult();
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                // Decodificar el token para obtener el TenantId sin validarlo aún
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken;

                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al leer el token JWT");
                    return AuthenticateResult.Fail("Token inválido");
                }

                // Obtener el TenantId del token
                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
                {
                    _logger.LogWarning("Token no contiene TenantId válido");
                    return AuthenticateResult.Fail("Token inválido: TenantId no encontrado");
                }

                // Obtener el secreto específico del tenant
                var jwtSecret = await _secretRepository.GetSecretValueAsync("JWT_SECRET", tenantId);

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    _logger.LogError("No se encontró secreto JWT para tenant {TenantId}", tenantId);
                    return AuthenticateResult.Fail("Configuración de seguridad inválida");
                }

                // Configurar los parámetros de validación
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = Options.TokenValidationParameters.ValidIssuer,
                    ValidAudience = Options.TokenValidationParameters.ValidAudience,
                    ClockSkew = TimeSpan.Zero
                };

                try
                {
                    // Validar el token con el secreto específico del tenant
                    var principal = handler.ValidateToken(
                        token,
                        validationParameters,
                        out var validatedToken);

                    // Crear el ticket de autenticación
                    var ticket = new AuthenticationTicket(
                        principal,
                        Scheme.Name);

                    _logger.LogInformation(
                        "Token validado exitosamente para tenant {TenantId}",
                        tenantId);

                    return AuthenticateResult.Success(ticket);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning(
                        "Token expirado para tenant {TenantId}",
                        tenantId);
                    return AuthenticateResult.Fail("Token expirado");
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    _logger.LogWarning(
                        "Firma inválida en token para tenant {TenantId}",
                        tenantId);
                    return AuthenticateResult.Fail("Firma del token inválida");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error validando token para tenant {TenantId}",
                        tenantId);
                    return AuthenticateResult.Fail(
                        "Error al validar el token: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en autenticación");
                return AuthenticateResult.Fail(
                    "Error interno en el proceso de autenticación");
            }
        }

        /// <summary>
        /// Maneja el desafío de autenticación cuando falla la autorización.
        /// </summary>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"Bearer error=\"invalid_token\"";
            await base.HandleChallengeAsync(properties);
        }

        /// <summary>
        /// Maneja el caso en que se prohíbe el acceso (forbidden).
        /// </summary>
        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"Bearer error=\"insufficient_permissions\"";
            await base.HandleForbiddenAsync(properties);
        }
    }
}
