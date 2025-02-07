using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.BL.Cache.Monitors;
using GenesisFEPortalWebApp.BL.Cache;
using GenesisFEPortalWebApp.BL.Cache.Configuration;
using System.Text;
using GenesisFEPortalWebApp.ApiService.Authentication;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();


// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mi API", Version = "v1" });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignorar referencias circulares
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Mantener las mayúsculas/minúsculas de las propiedades
        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // Ignorar valores nulos
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });


builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services 

// Catalog services
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// Customer services
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

//User services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHttpContextAccessor();

// Authentication services
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantRegistrationService, TenantRegistrationService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAuthAuditLogger, AuthAuditLogger>();


// Configuración de seguridad
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection("Security"));


// Registrar servicios relacionados con tenant y autenticación
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ISecretService, SecretService>();

builder.Services.AddScoped<ISecretRepository, SecretRepository>();


builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();


// Registrar el manejador de eventos personalizado
builder.Services.AddScoped<TenantJwtBearerEvents>();
builder.Services.AddScoped<IAuthenticationHandler, MultiTenantAuthenticationHandler>();
// Actualizar el registro del servicio de tokens
builder.Services.AddScoped<ITokenService, TokenService>();

// Agregar estas líneas después de builder.Services.AddControllers

// Configuración del caché
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.ConfigurationSection));

// Servicios de caché
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<CatalogChangeMonitor>();

// JWT Configuration
// Agregar autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddScheme<JwtBearerOptions, MultiTenantAuthenticationHandler>(
    JwtBearerDefaults.AuthenticationScheme,
    options => {
        var jwtConfig = builder.Configuration.GetSection("JWT");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["ValidIssuer"],
            ValidAudience = jwtConfig["ValidAudience"],           
        };
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers(); // Agregado

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Add authentication middleware to pipeline
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.Run();

