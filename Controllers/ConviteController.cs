using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConviteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConviteController> _logger;

        public ConviteController(AppDbContext context, ILogger<ConviteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Endpoints públicos ────────────────────────────────────────────────

        /// <summary>Retorna informações básicas do convite para exibição na página de aceite.</summary>
        [HttpGet("info/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInfo(string token)
        {
            var convite = await _context.ConvitesEmpresa
                .Include(c => c.Empresa)
                .Include(c => c.Cargo)
                .FirstOrDefaultAsync(c => c.Token == token);

            if (convite == null)
                return NotFound(new { message = "Convite não encontrado." });

            var valido = convite.Status == StatusConvite.Pendente
                      && convite.ExpiresAt > DateTime.UtcNow;

            return Ok(new ConviteInfoDTO
            {
                EmpresaNome = convite.Empresa.Nome,
                CargoNome   = convite.Cargo.Nome,
                Email       = convite.Email,
                ExpiresAt   = convite.ExpiresAt,
                Valido      = valido
            });
        }

        // ── Endpoints autenticados ────────────────────────────────────────────

        /// <summary>Envia um convite por e-mail para um futuro funcionário.</summary>
        [HttpPost]
        [Authorize(Policy = "GerenciarFuncionarios")]
        public async Task<IActionResult> Enviar([FromBody] ConviteRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            // Verifica se o cargo pertence à empresa
            var cargo = await _context.Cargos
                .FirstOrDefaultAsync(c => c.Id == dto.CargoId && c.EmpresaId == empresaId);
            if (cargo == null)
                return BadRequest(new { message = "Cargo não encontrado." });

            // Bloqueia se o e-mail já pertence a um funcionário da empresa
            var jaFuncionario = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.EmpresaId == empresaId);
            if (jaFuncionario)
                return Conflict(new { message = "Este e-mail já pertence a um funcionário da empresa." });

            // Bloqueia convite duplicado pendente
            var convitePendente = await _context.ConvitesEmpresa
                .AnyAsync(c => c.Email == dto.Email
                            && c.EmpresaId == empresaId
                            && c.Status == StatusConvite.Pendente
                            && c.ExpiresAt > DateTime.UtcNow);
            if (convitePendente)
                return Conflict(new { message = "Já existe um convite pendente para este e-mail." });

            var convite = new ConviteEmpresa
            {
                Email     = dto.Email,
                Token     = GerarToken(),
                EmpresaId = empresaId.Value,
                CargoId   = dto.CargoId,
                Status    = StatusConvite.Pendente,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.ConvitesEmpresa.Add(convite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Convite {ConviteId} enviado para {Email}.", convite.Id, convite.Email);

            // TODO: enviar e-mail com o link do convite
            // Por ora o link é retornado no response para ser compartilhado manualmente

            return Ok(new ConviteResponseDTO
            {
                Id        = convite.Id,
                Email     = convite.Email,
                CargoId   = convite.CargoId,
                CargoNome = cargo.Nome,
                Status    = convite.Status.ToString(),
                Token     = convite.Token,
                ExpiresAt = convite.ExpiresAt,
                CreatedAt = convite.CreatedAt
            });
        }

        /// <summary>Lista todos os convites da empresa.</summary>
        [HttpGet]
        [Authorize(Policy = "GerenciarFuncionarios")]
        public async Task<IActionResult> Listar([FromQuery] string? status)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var query = _context.ConvitesEmpresa
                .Where(c => c.EmpresaId == empresaId)
                .Include(c => c.Cargo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusConvite>(status, true, out var statusEnum))
                query = query.Where(c => c.Status == statusEnum);

            var convites = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ConviteResponseDTO
                {
                    Id        = c.Id,
                    Email     = c.Email,
                    CargoId   = c.CargoId,
                    CargoNome = c.Cargo.Nome,
                    Status    = c.Status.ToString(),
                    Token     = c.Token,
                    ExpiresAt = c.ExpiresAt,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(convites);
        }

        /// <summary>
        /// Usuário autenticado aceita o convite via token.
        /// Vincula o usuário à empresa com o cargo definido no convite.
        /// </summary>
        [HttpPost("{token}/aceitar")]
        [Authorize]
        public async Task<IActionResult> Aceitar(string token)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var convite = await _context.ConvitesEmpresa
                .Include(c => c.Empresa)
                .FirstOrDefaultAsync(c => c.Token == token);

            if (convite == null)
                return NotFound(new { message = "Convite não encontrado." });

            if (convite.Status != StatusConvite.Pendente)
                return BadRequest(new { message = "Este convite já foi utilizado ou cancelado." });

            if (convite.ExpiresAt < DateTime.UtcNow)
            {
                convite.Status = StatusConvite.Expirado;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "Este convite expirou." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (user.Email?.ToLower() != convite.Email.ToLower())
                return BadRequest(new { message = "Este convite foi enviado para outro e-mail." });

            if (user.EmpresaId != null)
                return Conflict(new { message = "Você já pertence a uma empresa." });

            user.EmpresaId = convite.EmpresaId;
            user.CargoId   = convite.CargoId;
            convite.Status = StatusConvite.Aceito;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} aceitou convite e entrou na empresa {EmpresaId}.", userId, convite.EmpresaId);
            return Ok(new { message = $"Bem-vindo à {convite.Empresa.Nome}!" });
        }

        /// <summary>Cancela/remove um convite pendente.</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "GerenciarFuncionarios")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var convite = await _context.ConvitesEmpresa
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (convite == null) return NotFound();

            _context.ConvitesEmpresa.Remove(convite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirst("empresaId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static string GerarToken()
            => Guid.NewGuid().ToString("N"); // 32 caracteres hex
    }
}
