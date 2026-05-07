using MenuBuilderBack.Authorization;
using MenuBuilderBack.Data;
using MenuBuilderBack.Extensions;
using MenuBuilderBack.Middleware;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Enums;
using MenuBuilderBack.Models.Settings;
using MenuBuilderBack.Service;
using MenuBuilderBack.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");

// Railway injeta PORT dinamicamente; em dev usa a porta padrão do launchSettings
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connectionString = builder.Configuration.GetConnectionString("SupabaseConnection");
if (string.IsNullOrEmpty(connectionString))
{
    logger.LogCritical("Connection string 'SupabaseConnection' não encontrada.");
    throw new InvalidOperationException("Configuração de banco de dados ausente.");
}

// CORS: AllowAnyOrigin globalmente — segurança garantida pelo JWT, não por restrição de origem.
// Necessário para o cardápio público (QR code de qualquer dispositivo) e para o admin em dev/prod.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Rate limiting para endpoints públicos (60 req/min por IP)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit            = 60;
        opt.Window                 = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit             = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddRepositories();
builder.Services.AddServices();

var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = jwtSection.Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    if (builder.Environment.IsProduction())
    {
        logger.LogCritical("JwtSettings:SecretKey não configurado em produção.");
        throw new InvalidOperationException("Configuração de JWT ausente.");
    }
    jwtSettings = new JwtSettings { SecretKey = "dGVzdGVjaGF2ZXRlbXBvcmFyaWE=" };
    logger.LogWarning("Usando chave JWT temporária para desenvolvimento.");
}

byte[] key;
try
{
    key = Convert.FromBase64String(jwtSettings.SecretKey);
}
catch (FormatException ex)
{
    logger.LogCritical(ex, "JwtSettings:SecretKey não é um Base64 válido.");
    if (builder.Environment.IsProduction()) throw;
    key = Convert.FromBase64String("dGVzdGVjaGF2ZXRlbXBvcmFyaWE=");
}

builder.Services.Configure<JwtSettings>(jwtSection);

// ── Autorização baseada em permissões de cargo ───────────────────────────
builder.Services.AddScoped<IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (Permissao permissao in Enum.GetValues<Permissao>())
    {
        options.AddPolicy(permissao.ToString(), policy =>
            policy.Requirements.Add(new PermissaoRequirement(permissao)));
    }
});

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MenuBuilder API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite seu token JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MenuBuilder API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseRouting();       // deve vir antes de UseCors para que [EnableCors] funcione
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

logger.LogInformation("Aplicação iniciada com sucesso.");
app.Run();

// Expõe a classe Program para o projeto de testes via WebApplicationFactory
public partial class Program { }
