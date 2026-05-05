using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MenuBuilderBack.Data;
using MenuBuilderBack.Helpers;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, UserManager<User> userManager,
            IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            // 1. Verifica e-mail duplicado
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "Este e-mail já está sendo usado." });

            // ── Fluxo via convite ─────────────────────────────────────────────
            if (!string.IsNullOrEmpty(model.ConviteToken))
            {
                var convite = await _context.ConvitesEmpresa
                    .Include(c => c.Empresa)
                    .FirstOrDefaultAsync(c => c.Token == model.ConviteToken);

                if (convite == null)
                    return BadRequest(new { message = "Convite inválido." });
                if (convite.Status != StatusConvite.Pendente)
                    return BadRequest(new { message = "Este convite já foi utilizado ou cancelado." });
                if (convite.ExpiresAt < DateTime.UtcNow)
                    return BadRequest(new { message = "Este convite expirou." });
                if (convite.Email.ToLower() != model.Email.ToLower())
                    return BadRequest(new { message = "Este convite foi enviado para outro e-mail." });

                var user = new User
                {
                    Email        = model.Email,
                    UserName     = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    IsOwner      = false,
                    EmpresaId    = convite.EmpresaId,
                    CargoId      = convite.CargoId,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);

                convite.Status = StatusConvite.Aceito;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário {UserId} entrou na empresa {EmpresaId} via convite.", user.Id, convite.EmpresaId);
                return Ok(new { message = $"Bem-vindo à {convite.Empresa.Nome}!" });
            }

            // ── Fluxo de criação de empresa ───────────────────────────────────
            if (string.IsNullOrWhiteSpace(model.NomeEmpresa))
                return BadRequest(new { message = "O nome da empresa é obrigatório." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var empresa = new Empresa
                {
                    Nome          = model.NomeEmpresa,
                    CodigoConvite = GerarCodigoConvite(),
                    Slug          = await SlugHelper.GerarUnico(model.NomeEmpresa, _context)
                };
                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();

                var owner = new User
                {
                    Email        = model.Email,
                    UserName     = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    IsOwner      = true,
                    EmpresaId    = empresa.Id,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(owner, model.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(result.Errors);
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Empresa {EmpresaId} e usuário {UserId} criados.", empresa.Id, owner.Id);
                return Ok(new { message = "Conta e empresa criadas com sucesso!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao registrar usuário e empresa.");
                return StatusCode(500, new { message = "Erro interno ao criar a conta. Tente novamente." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var passwordValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!passwordValid)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.NomeCompleto,
                    user.Email,
                    user.EmpresaId,
                    user.IsOwner,
                    user.CargoId
                }
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _config["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

            var key = Convert.FromBase64String(secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.NomeCompleto ?? ""),
                new("empresaId", user.EmpresaId?.ToString() ?? ""),
                new("isOwner", user.IsOwner.ToString().ToLower()),
                new("cargoId", user.CargoId?.ToString() ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string GerarCodigoConvite()
            => Guid.NewGuid().ToString("N")[..8].ToUpper(); // ex: "A3F2B1C9"
    }
}
