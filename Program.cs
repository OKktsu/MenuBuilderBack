using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

try 
{
    // --- LOGS DE DEBUG PARA O AZURE (Aparecerão no Fluxo de Log) ---
    Console.WriteLine("[STARTUP] Iniciando carregamento de configurações...");

    // 1. Configurar Conexão com Supabase (PostgreSQL)
    // No Azure, use a variável: ConnectionStrings__SupabaseConnection
    var connectionString = builder.Configuration["ConnectionStrings:SupabaseConnection"];
    
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("[ERRO] Connection String 'SupabaseConnection' não encontrada!");
        throw new Exception("Configuração de Banco de Dados ausente no Azure.");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // 2. Configurar JWT com Verificação de Segurança
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    var jwtSettings = jwtSection.Get<JwtSettings>();

    if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
    {
        Console.WriteLine("[ERRO] Seção 'JwtSettings' ou 'SecretKey' não encontrada!");
        throw new Exception("Configuração de JWT ausente no Azure.");
    }

    // Tenta converter a chave Base64 (Isso causa Erro 500 se a chave estiver mal formatada no Azure)
    byte[] key;
    try {
        key = Convert.FromBase64String(jwtSettings.SecretKey);
    } catch (Exception ex) {
        Console.WriteLine($"[ERRO] A SecretKey no Azure não é um Base64 válido: {ex.Message}");
        throw;
    }

    builder.Services.Configure<JwtSettings>(jwtSection);

    builder.Services.AddAuthentication(x => {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x => {
        x.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

    // --- RESTANTE DOS SERVIÇOS ---
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MenuBuilder API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Digite seu token JWT: **Bearer {seu_token}**"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddIdentity<User, IdentityRole>(options => {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    var app = builder.Build();

    // --- MIDDLEWARES E PIPELINE ---
    
    // Swagger visível em Produção (Azure) para facilitar seu teste
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MenuBuilder API V1");
        c.RoutePrefix = string.Empty; 
    });

    app.UseCors(policy => 
        policy.WithOrigins("http://localhost:4200", "https://menu-builder-front.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader());

    app.UseHttpsRedirection();
    app.UseAuthentication(); 
    app.UseAuthorization();
    app.MapControllers();

    Console.WriteLine("[STARTUP] Aplicação configurada com sucesso. Rodando...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("====================================================");
    Console.WriteLine("CRITICAL ERROR DURING APPLICATION STARTUP:");
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
    Console.WriteLine("====================================================");
    throw; // Garante que o Azure registre a falha
}