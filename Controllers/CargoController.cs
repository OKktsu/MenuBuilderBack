using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Authorize(Policy = "GerenciarCargos")]
    [Route("api/[controller]")]
    public class CargoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CargoController> _logger;

        public CargoController(AppDbContext context, ILogger<CargoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Lista todos os cargos da empresa do usuário autenticado.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var cargos = await _context.Cargos
                .Where(c => c.EmpresaId == empresaId)
                .Include(c => c.Permissoes)
                .Include(c => c.Usuarios)
                .OrderBy(c => c.Nome)
                .Select(c => new CargoResponseDTO
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    EmpresaId = c.EmpresaId,
                    Permissoes = c.Permissoes.Select(p => p.Permissao.ToString()).ToList(),
                    TotalFuncionarios = c.Usuarios.Count
                })
                .ToListAsync();

            return Ok(cargos);
        }

        /// <summary>Retorna um cargo específico com suas permissões.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var cargo = await _context.Cargos
                .Include(c => c.Permissoes)
                .Include(c => c.Usuarios)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cargo == null) return NotFound();

            return Ok(new CargoResponseDTO
            {
                Id = cargo.Id,
                Nome = cargo.Nome,
                EmpresaId = cargo.EmpresaId,
                Permissoes = cargo.Permissoes.Select(p => p.Permissao.ToString()).ToList(),
                TotalFuncionarios = cargo.Usuarios.Count
            });
        }

        /// <summary>Cria um novo cargo com as permissões definidas.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CargoRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var nomeJaExiste = await _context.Cargos
                .AnyAsync(c => c.EmpresaId == empresaId && c.Nome.ToLower() == dto.Nome.ToLower());
            if (nomeJaExiste)
                return Conflict(new { message = "Já existe um cargo com este nome." });

            var cargo = new Cargo
            {
                Nome = dto.Nome,
                EmpresaId = empresaId.Value,
                Permissoes = dto.Permissoes
                    .Distinct()
                    .Select(p => new CargoPermissao { Permissao = p })
                    .ToList()
            };

            _context.Cargos.Add(cargo);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo {CargoId} ({Nome}) criado para empresa {EmpresaId}.", cargo.Id, cargo.Nome, empresaId);

            return CreatedAtAction(nameof(GetById), new { id = cargo.Id }, new CargoResponseDTO
            {
                Id = cargo.Id,
                Nome = cargo.Nome,
                EmpresaId = cargo.EmpresaId,
                Permissoes = cargo.Permissoes.Select(p => p.Permissao.ToString()).ToList(),
                TotalFuncionarios = 0
            });
        }

        /// <summary>Atualiza nome e permissões de um cargo.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CargoRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var cargo = await _context.Cargos
                .Include(c => c.Permissoes)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cargo == null) return NotFound();

            var nomeJaExiste = await _context.Cargos
                .AnyAsync(c => c.EmpresaId == empresaId && c.Nome.ToLower() == dto.Nome.ToLower() && c.Id != id);
            if (nomeJaExiste)
                return Conflict(new { message = "Já existe outro cargo com este nome." });

            cargo.Nome = dto.Nome;
            cargo.UpdatedAt = DateTime.UtcNow;

            // Substitui as permissões completamente
            _context.CargoPermissoes.RemoveRange(cargo.Permissoes);
            cargo.Permissoes = dto.Permissoes
                .Distinct()
                .Select(p => new CargoPermissao { Permissao = p, CargoId = cargo.Id })
                .ToList();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo {CargoId} atualizado.", cargo.Id);
            return NoContent();
        }

        /// <summary>Remove um cargo (somente se não houver funcionários vinculados).</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var cargo = await _context.Cargos
                .Include(c => c.Usuarios)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cargo == null) return NotFound();

            if (cargo.Usuarios.Count > 0)
                return Conflict(new { message = $"Este cargo possui {cargo.Usuarios.Count} funcionário(s) vinculado(s). Remova-os antes de excluir." });

            _context.Cargos.Remove(cargo);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cargo {CargoId} deletado.", id);
            return NoContent();
        }

        // ── Helper ──────────────────────────────────��─────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirst("empresaId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
