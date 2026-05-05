using MenuBuilderBack.Data;
using MenuBuilderBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    /// <summary>
    /// Endpoints públicos do cardápio — sem autenticação, sem dados internos.
    /// Nunca expõe IDs, funcionários, hierarquia ou qualquer dado operacional.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/public")]
    [EnableRateLimiting("public")]
    public class PublicMenuController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PublicMenuController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Informações básicas do restaurante (nome, logo, horário).</summary>
        [HttpGet("{slug}/info")]
        public async Task<IActionResult> GetInfo(string slug)
        {
            var info = await _context.Empresas
                .Where(e => e.Slug == slug && e.IsActive)
                .Select(e => new PublicRestaurantInfoDTO
                {
                    RestaurantName = e.Nome,
                    LogoUrl        = e.LogoUrl,
                    OpeningHours   = e.Menus
                        .Where(m => m.IsActive)
                        .Select(m => m.OpeningHours)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            // Retorna 404 igual a "não existe" — não revela se a empresa está inativa
            if (info == null) return NotFound();
            return Ok(info);
        }

        /// <summary>Cardápio completo resolvido: menus próprios + herdados, com overrides aplicados.</summary>
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetCardapio(string slug)
        {
            var empresa = await _context.Empresas
                .Where(e => e.Slug == slug && e.IsActive)
                .Select(e => new { e.Id, e.Nome, e.LogoUrl, e.EmpresaMaeId })
                .FirstOrDefaultAsync();

            if (empresa == null) return NotFound();

            var menuIds = await ColetarMenuIdsElegiveis(empresa.Id, empresa.EmpresaMaeId);
            var empresaId = empresa.Id;

            // Projeta menus com overrides aplicados diretamente na query
            var menus = await _context.Menus
                .Where(m => menuIds.Contains(m.Id) && m.IsActive)
                .OrderBy(m => m.Id)
                .Select(m => new PublicMenuDTO
                {
                    Name         = m.RestaurantName,
                    OpeningHours = m.OpeningHours,
                    Categories   = m.Categories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Order)
                        .Select(c => new PublicCategoryDTO
                        {
                            Name  = c.Name,
                            Items = c.Items
                                .Where(i => i.IsActive
                                    && !_context.MenuItemOverrides.Any(o =>
                                        o.EmpresaId == empresaId
                                        && o.MenuItemId == i.Id
                                        && !o.IsActive))
                                .OrderBy(i => i.Order)
                                .Select(i => new PublicMenuItemDTO
                                {
                                    Name        = i.Name,
                                    Description = i.Description,
                                    Ingredients = i.Ingredients,
                                    Price       = (decimal)(_context.MenuItemOverrides
                                        .Where(o => o.EmpresaId == empresaId && o.MenuItemId == i.Id && o.Price != null)
                                        .Select(o => (decimal?)o.Price)
                                        .FirstOrDefault() ?? i.Price),
                                    ImageUrl    = i.ImagePath,
                                    Tags        = i.Tags
                                })
                                .ToList()
                        })
                        .Where(c => c.Items.Count > 0)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new PublicMenuResponseDTO
            {
                RestaurantName = empresa.Nome,
                LogoUrl        = empresa.LogoUrl,
                Menus          = menus
            });
        }

        // ── Helper privado ───────────────────────────────────────────────────

        private async Task<HashSet<int>> ColetarMenuIdsElegiveis(int empresaId, int? empresaMaeId)
        {
            var ids = new HashSet<int>();

            var proprios = await _context.Menus
                .Where(m => m.EmpresaId == empresaId && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync();
            ids.UnionWith(proprios);

            // Sobe a hierarquia coletando menus compartilhados
            var maeAtualId = empresaMaeId;
            while (maeAtualId.HasValue)
            {
                var mae = await _context.Empresas
                    .Where(e => e.Id == maeAtualId.Value && e.IsActive)
                    .Select(e => new { e.Id, e.EmpresaMaeId })
                    .FirstOrDefaultAsync();

                if (mae == null) break;

                var compartilhados = await _context.Menus
                    .Where(m => m.EmpresaId == mae.Id && m.Compartilhado && m.IsActive)
                    .Select(m => m.Id)
                    .ToListAsync();

                ids.UnionWith(compartilhados);
                maeAtualId = mae.EmpresaMaeId;
            }

            return ids;
        }
    }
}
