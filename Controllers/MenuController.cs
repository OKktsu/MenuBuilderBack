using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MenuController> _logger;

        public MenuController(AppDbContext context, ILogger<MenuController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menus = await _context.Menus
                .Where(m => m.EmpresaId == empresaId)
                .Select(m => new MenuResponseDTO
                {
                    Id = m.Id,
                    RestaurantName = m.RestaurantName,
                    Description = m.Description,
                    OpeningHours = m.OpeningHours,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(menus);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = await _context.Menus
                .Where(m => m.Id == id && m.EmpresaId == empresaId)
                .Select(m => new MenuResponseDTO
                {
                    Id = m.Id,
                    RestaurantName = m.RestaurantName,
                    Description = m.Description,
                    OpeningHours = m.OpeningHours,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (menu == null) return NotFound();
            return Ok(menu);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = await _context.Menus
                .Where(m => m.Id == id && m.EmpresaId == empresaId)
                .Include(m => m.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Items.OrderBy(i => i.Order))
                .FirstOrDefaultAsync();

            if (menu == null) return NotFound();

            var response = new MenuResponseDTO
            {
                Id = menu.Id,
                RestaurantName = menu.RestaurantName,
                Description = menu.Description,
                OpeningHours = menu.OpeningHours,
                CreatedAt = menu.CreatedAt,
                Categories = menu.Categories.Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order,
                    MenuId = c.MenuId,
                    Items = c.Items.Select(i => new MenuItemResponseDTO
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Description = i.Description,
                        Order = i.Order,
                        Ingredients = i.Ingredients,
                        Price = i.Price,
                        ImagePath = i.ImagePath,
                        Tags = i.Tags
                    }).ToList()
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Policy = "CriarMenu")]
        public async Task<IActionResult> Create([FromBody] MenuRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = new Menu
            {
                RestaurantName = dto.RestaurantName,
                Description = dto.Description,
                OpeningHours = dto.OpeningHours,
                EmpresaId = empresaId.Value
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} criado pela empresa {EmpresaId}", menu.Id, empresaId);
            return CreatedAtAction(nameof(GetById), new { id = menu.Id }, new MenuResponseDTO
            {
                Id = menu.Id,
                RestaurantName = menu.RestaurantName,
                Description = menu.Description,
                OpeningHours = menu.OpeningHours,
                CreatedAt = menu.CreatedAt
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Update(int id, [FromBody] MenuRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);
            if (menu == null) return NotFound();

            menu.RestaurantName = dto.RestaurantName;
            menu.Description = dto.Description;
            menu.OpeningHours = dto.OpeningHours;
            menu.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DeletarMenu")]
        public async Task<IActionResult> Delete(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);
            if (menu == null) return NotFound();

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} deletado", id);
            return NoContent();
        }

        /// <summary>Alterna o compartilhamento do menu com empresas filhas. Exclusivo para donos.</summary>
        [HttpPut("{id}/compartilhar")]
        public async Task<IActionResult> Compartilhar(int id)
        {
            var isOwner = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) != null
                && User.FindFirst("isOwner")?.Value == "true";
            if (!isOwner) return Forbid();

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);
            if (menu == null) return NotFound();

            menu.Compartilhado = !menu.Compartilhado;
            menu.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} compartilhado={Compartilhado}", id, menu.Compartilhado);
            return Ok(new { menu.Id, menu.Compartilhado });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirstValue("empresaId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
