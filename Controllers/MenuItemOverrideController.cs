using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MenuItemOverrideController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MenuItemOverrideController> _logger;

        public MenuItemOverrideController(AppDbContext context, ILogger<MenuItemOverrideController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Cria ou atualiza o override de um item para a empresa autenticada.
        /// Permite ajustar preço ou ocultar o item no cardápio da unidade.
        /// </summary>
        [HttpPut("{menuItemId}")]
        public async Task<IActionResult> Upsert(int menuItemId, [FromBody] MenuItemOverrideRequestDTO dto)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            // Garante que o item existe e pertence ao grupo (empresa própria ou mãe com menu compartilhado)
            var itemExiste = await _context.MenuItems.AnyAsync(i => i.Id == menuItemId && i.IsActive);
            if (!itemExiste) return NotFound(new { message = "Item não encontrado." });

            var override_ = await _context.MenuItemOverrides
                .FirstOrDefaultAsync(o => o.EmpresaId == empresaId && o.MenuItemId == menuItemId);

            if (override_ == null)
            {
                override_ = new MenuItemOverride
                {
                    EmpresaId  = empresaId.Value,
                    MenuItemId = menuItemId,
                    Price      = dto.Price,
                    IsActive   = dto.IsActive
                };
                _context.MenuItemOverrides.Add(override_);
            }
            else
            {
                override_.Price    = dto.Price;
                override_.IsActive = dto.IsActive;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Override do item {MenuItemId} atualizado pela empresa {EmpresaId}.", menuItemId, empresaId);

            return Ok(new { override_.MenuItemId, override_.Price, override_.IsActive });
        }

        /// <summary>Remove o override, fazendo o item voltar ao preço e visibilidade originais.</summary>
        [HttpDelete("{menuItemId}")]
        public async Task<IActionResult> Delete(int menuItemId)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var override_ = await _context.MenuItemOverrides
                .FirstOrDefaultAsync(o => o.EmpresaId == empresaId && o.MenuItemId == menuItemId);

            if (override_ == null) return NotFound();

            _context.MenuItemOverrides.Remove(override_);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Override do item {MenuItemId} removido pela empresa {EmpresaId}.", menuItemId, empresaId);
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
