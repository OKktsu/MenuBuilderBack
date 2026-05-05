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
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AppDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var categories = await _context.Categories
                .Where(c => c.Menu.EmpresaId == empresaId)
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order,
                    MenuId = c.MenuId
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Items.OrderBy(i => i.Order))
                .FirstOrDefaultAsync(c => c.Id == id && c.Menu.EmpresaId == empresaId);

            if (category == null) return NotFound();

            return Ok(new CategoryResponseDTO
            {
                Id = category.Id,
                Name = category.Name,
                Order = category.Order,
                MenuId = category.MenuId,
                Items = category.Items.Select(i => new MenuItemResponseDTO
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Order = i.Order,
                    Price = i.Price,
                    ImagePath = i.ImagePath,
                    Tags = i.Tags
                }).ToList()
            });
        }

        [HttpGet("menu/{menuId}")]
        public async Task<IActionResult> GetByMenu(int menuId)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menuExists = await _context.Menus.AnyAsync(m => m.Id == menuId && m.EmpresaId == empresaId);
            if (!menuExists) return NotFound(new { message = "Menu não encontrado." });

            var categories = await _context.Categories
                .Where(c => c.MenuId == menuId)
                .OrderBy(c => c.Order)
                .Include(c => c.Items.OrderBy(i => i.Order))
                .Select(c => new CategoryResponseDTO
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
                        Price = i.Price,
                        ImagePath = i.ImagePath,
                        Tags = i.Tags
                    }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Create([FromBody] CategoryRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var menuExists = await _context.Menus.AnyAsync(m => m.Id == dto.MenuId && m.EmpresaId == empresaId);
            if (!menuExists) return BadRequest(new { message = "Menu não encontrado." });

            var category = new Category
            {
                Name = dto.Name,
                Order = dto.Order,
                MenuId = dto.MenuId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new CategoryResponseDTO
            {
                Id = category.Id,
                Name = category.Name,
                Order = category.Order,
                MenuId = category.MenuId
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.Menu.EmpresaId == empresaId);

            if (category == null) return NotFound();

            category.Name = dto.Name;
            category.Order = dto.Order;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DeletarMenu")]
        public async Task<IActionResult> Delete(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.Menu.EmpresaId == empresaId);

            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Categoria {CategoryId} deletada", id);
            return NoContent();
        }

        [HttpPost("{categoryId}/items/{itemId}")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> AddItem(int categoryId, int itemId)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.Menu.EmpresaId == empresaId);

            if (category == null) return NotFound(new { message = "Categoria não encontrada." });

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.EmpresaId == empresaId);

            if (item == null) return NotFound(new { message = "Item não encontrado." });

            if (category.Items.Any(i => i.Id == itemId))
                return Conflict(new { message = "Item já pertence a esta categoria." });

            category.Items.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new MenuItemResponseDTO
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Order = item.Order,
                Price = item.Price,
                Ingredients = item.Ingredients,
                ImagePath = item.ImagePath,
                Tags = item.Tags
            });
        }

        [HttpDelete("{categoryId}/items/{itemId}")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> RemoveItem(int categoryId, int itemId)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.Menu.EmpresaId == empresaId);

            if (category == null) return NotFound(new { message = "Categoria não encontrada." });

            var item = category.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return NotFound(new { message = "Item não encontrado nesta categoria." });

            category.Items.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("reorder")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Reorder([FromBody] List<ReorderItemDTO> items)
        {
            if (items == null || items.Count == 0) return BadRequest();

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var ids = items.Select(i => i.Id).ToList();
            var categories = await _context.Categories
                .Where(c => ids.Contains(c.Id) && c.Menu.EmpresaId == empresaId)
                .ToListAsync();

            foreach (var category in categories)
            {
                var dto = items.First(i => i.Id == category.Id);
                category.Order = dto.Order;
                category.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
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
