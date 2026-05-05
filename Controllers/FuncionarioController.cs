using MenuBuilderBack.Data;
using MenuBuilderBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Authorize(Policy = "GerenciarFuncionarios")]
    [Route("api/[controller]")]
    public class FuncionarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FuncionarioController> _logger;

        public FuncionarioController(AppDbContext context, ILogger<FuncionarioController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Lista todos os funcionários da empresa do usuário autenticado.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var funcionarios = await _context.Users
                .Where(u => u.EmpresaId == empresaId)
                .Include(u => u.Cargo)
                .OrderBy(u => u.NomeCompleto)
                .Select(u => new FuncionarioResponseDTO
                {
                    UserId      = u.Id,
                    NomeCompleto = u.NomeCompleto,
                    Email       = u.Email ?? "",
                    IsOwner     = u.IsOwner,
                    CargoId     = u.CargoId,
                    CargoNome   = u.Cargo != null ? u.Cargo.Nome : null,
                    DataEntrada = u.DataCadastro
                })
                .ToListAsync();

            return Ok(funcionarios);
        }

        /// <summary>Altera o cargo de um funcionário.</summary>
        [HttpPut("{userId}/cargo")]
        public async Task<IActionResult> AlterarCargo(string userId, [FromBody] AlterarCargoDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var funcionario = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.EmpresaId == empresaId);

            if (funcionario == null) return NotFound(new { message = "Funcionário não encontrado." });
            if (funcionario.IsOwner) return BadRequest(new { message = "O cargo do dono não pode ser alterado." });

            var cargoExiste = await _context.Cargos
                .AnyAsync(c => c.Id == dto.CargoId && c.EmpresaId == empresaId);
            if (!cargoExiste) return BadRequest(new { message = "Cargo não encontrado." });

            funcionario.CargoId = dto.CargoId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo do usuário {UserId} alterado para {CargoId}.", userId, dto.CargoId);
            return NoContent();
        }

        /// <summary>Remove um funcionário da empresa (não deleta a conta, apenas desvincula).</summary>
        [HttpDelete("{userId}")]
        public async Task<IActionResult> Remover(string userId)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var funcionario = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.EmpresaId == empresaId);

            if (funcionario == null) return NotFound(new { message = "Funcionário não encontrado." });
            if (funcionario.IsOwner) return BadRequest(new { message = "O dono da empresa não pode ser removido." });

            funcionario.EmpresaId = null;
            funcionario.CargoId   = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} removido da empresa {EmpresaId}.", userId, empresaId);
            return NoContent();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirst("empresaId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
