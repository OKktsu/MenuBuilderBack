using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MenuItemController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPhotoService _photoService;
        private readonly ILogger<MenuItemController> _logger;

        public MenuItemController(AppDbContext context, IPhotoService photoService, ILogger<MenuItemController> logger)
        {
            _context = context;
            _photoService = photoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var items = await _context.MenuItems
                .Where(i => i.EmpresaId == empresaId)
                .Select(i => new MenuItemResponseDTO
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Order = i.Order,
                    Ingredients = i.Ingredients,
                    Price = i.Price,
                    ImagePath = i.ImagePath,
                    Tags = i.Tags
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(i => i.Id == id && i.EmpresaId == empresaId);

            if (item == null) return NotFound();

            return Ok(new MenuItemResponseDTO
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Order = item.Order,
                Ingredients = item.Ingredients,
                Price = item.Price,
                ImagePath = item.ImagePath,
                Tags = item.Tags
            });
        }

        [HttpPost]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Create([FromBody] MenuItemRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var item = new MenuItem
            {
                Name = dto.Name,
                Description = dto.Description,
                Order = dto.Order,
                Ingredients = dto.Ingredients,
                Price = dto.Price,
                Tags = dto.Tags,
                ImagePath = dto.ImagePath,
                EmpresaId = empresaId.Value
            };

            if (!string.IsNullOrEmpty(item.ImagePath) && item.ImagePath.StartsWith("data:image"))
            {
                var result = await _photoService.AddPhotoAsync(item.ImagePath);
                if (result.Error != null) return BadRequest(new { message = result.Error.Message });
                item.ImagePath = result.SecureUrl.AbsoluteUri;
            }

            if (dto.CategoryIds.Count > 0)
            {
                var categories = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id) && c.Menu.EmpresaId == empresaId)
                    .ToListAsync();
                item.Categories = categories;
            }

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new MenuItemResponseDTO
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Order = item.Order,
                Ingredients = item.Ingredients,
                Price = item.Price,
                ImagePath = item.ImagePath,
                Tags = item.Tags
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "EditarMenu")]
        public async Task<IActionResult> Update(int id, [FromBody] MenuItemRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var item = await _context.MenuItems
                .Include(i => i.Categories)
                .FirstOrDefaultAsync(i => i.Id == id && i.EmpresaId == empresaId);

            if (item == null) return NotFound();

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.Order = dto.Order;
            item.Ingredients = dto.Ingredients;
            item.Price = dto.Price;
            item.Tags = dto.Tags;
            item.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.ImagePath) && dto.ImagePath.StartsWith("data:image"))
            {
                var result = await _photoService.AddPhotoAsync(dto.ImagePath);
                if (result.Error != null) return BadRequest(new { message = result.Error.Message });
                item.ImagePath = result.SecureUrl.AbsoluteUri;
            }
            else if (dto.ImagePath != null)
            {
                item.ImagePath = dto.ImagePath;
            }

            if (dto.CategoryIds.Count > 0)
            {
                var categories = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id) && c.Menu.EmpresaId == empresaId)
                    .ToListAsync();
                item.Categories = categories;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DeletarMenu")]
        public async Task<IActionResult> Delete(int id)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(i => i.Id == id && i.EmpresaId == empresaId);

            if (item == null) return NotFound();

            if (!string.IsNullOrEmpty(item.ImagePath) && item.ImagePath.Contains("cloudinary"))
            {
                var publicId = ExtractCloudinaryPublicId(item.ImagePath);
                if (!string.IsNullOrEmpty(publicId))
                    await _photoService.DeletePhotoAsync(publicId);
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("MenuItem {ItemId} deletado", id);
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
            var menuItems = await _context.MenuItems
                .Where(i => ids.Contains(i.Id) && i.EmpresaId == empresaId)
                .ToListAsync();

            foreach (var menuItem in menuItems)
            {
                var dto = items.First(i => i.Id == menuItem.Id);
                menuItem.Order = dto.Order;
                menuItem.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirst("empresaId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static string ExtractCloudinaryPublicId(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0 || uploadIndex + 2 >= segments.Length) return string.Empty;

            var pathAfterVersion = string.Join("/", segments.Skip(uploadIndex + 2));
            return System.IO.Path.ChangeExtension(pathAfterVersion, null);
        }
    }
}
