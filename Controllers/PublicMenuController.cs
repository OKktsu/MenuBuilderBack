using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Controllers
{
    /// <summary>
    /// Endpoints públicos do cardápio — sem autenticação, sem dados internos.
    /// Nunca expõe IDs de empresa, funcionários, hierarquia ou dados operacionais.
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

        // ── Cardápio ─────────────────────────────────────────────────────────

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
                                    Id          = i.Id,
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

        // ── Sessão de Mesa ───────────────────────────────────────────────────

        /// <summary>Abre uma sessão de mesa. Retorna o token que identifica a sessão.</summary>
        [HttpPost("{slug}/sessao")]
        public async Task<IActionResult> AbrirSessao(string slug, [FromBody] AbrirSessaoRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = await _context.Empresas
                .Where(e => e.Slug == slug && e.IsActive)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (empresaId == null) return NotFound();

            var sessao = new SessaoMesa
            {
                EmpresaId   = empresaId.Value,
                NumeroMesa  = dto.NumeroMesa
            };

            _context.SessoesMesa.Add(sessao);
            await _context.SaveChangesAsync();

            return Ok(new SessaoResponseDTO
            {
                Token      = sessao.Token,
                NumeroMesa = sessao.NumeroMesa,
                Status     = sessao.Status,
                AbertoEm   = sessao.AbertoEm,
                Pedidos    = []
            });
        }

        /// <summary>Retorna a sessão com todos os pedidos e o total acumulado.</summary>
        [HttpGet("{slug}/sessao/{token:guid}")]
        public async Task<IActionResult> GetSessao(string slug, Guid token)
        {
            var sessao = await _context.SessoesMesa
                .Where(s => s.Token == token && s.Empresa.Slug == slug && s.Empresa.IsActive)
                .Include(s => s.Pedidos)
                    .ThenInclude(p => p.Itens)
                .FirstOrDefaultAsync();

            if (sessao == null) return NotFound();

            return Ok(MapSessaoResponse(sessao));
        }

        /// <summary>Adiciona um pedido à sessão. A sessão deve estar aberta.</summary>
        [HttpPost("{slug}/sessao/{token:guid}/pedido")]
        public async Task<IActionResult> FazerPedido(string slug, Guid token, [FromBody] NovoPedidoRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var sessao = await _context.SessoesMesa
                .Where(s => s.Token == token && s.Empresa.Slug == slug && s.Empresa.IsActive)
                .Include(s => s.Empresa)
                .FirstOrDefaultAsync();

            if (sessao == null) return NotFound();
            if (sessao.Status == StatusSessao.Encerrada)
                return Conflict(new { message = "Sessão encerrada. Não é possível adicionar pedidos." });

            var menuItemIds = dto.Itens.Select(i => i.MenuItemId).ToList();
            var itensDb = await _context.MenuItems
                .Where(i => menuItemIds.Contains(i.Id) && i.EmpresaId == sessao.EmpresaId && i.IsActive)
                .ToListAsync();

            if (itensDb.Count != dto.Itens.Count)
                return BadRequest(new { message = "Um ou mais itens são inválidos para este restaurante." });

            // Resolve preços com override aplicado
            var overrides = await _context.MenuItemOverrides
                .Where(o => o.EmpresaId == sessao.EmpresaId && menuItemIds.Contains(o.MenuItemId))
                .ToListAsync();

            var pedido = new Pedido
            {
                SessaoMesaId = sessao.Id,
                Itens = dto.Itens.Select(req =>
                {
                    var item = itensDb.First(i => i.Id == req.MenuItemId);
                    var ov   = overrides.FirstOrDefault(o => o.MenuItemId == req.MenuItemId);
                    return new PedidoItem
                    {
                        MenuItemId     = item.Id,
                        NomeItem       = item.Name,
                        PrecoUnitario  = ov?.Price ?? item.Price,
                        Quantidade     = req.Quantidade,
                        Observacao     = req.Observacao
                    };
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            return Ok(MapPedidoResponse(pedido));
        }

        /// <summary>Encerra a sessão e retorna o resumo final com o total geral.</summary>
        [HttpPut("{slug}/sessao/{token:guid}/encerrar")]
        public async Task<IActionResult> EncerrarSessao(string slug, Guid token)
        {
            var sessao = await _context.SessoesMesa
                .Where(s => s.Token == token && s.Empresa.Slug == slug && s.Empresa.IsActive)
                .Include(s => s.Pedidos)
                    .ThenInclude(p => p.Itens)
                .FirstOrDefaultAsync();

            if (sessao == null) return NotFound();
            if (sessao.Status == StatusSessao.Encerrada)
                return Conflict(new { message = "Sessão já encerrada." });

            sessao.Status      = StatusSessao.Encerrada;
            sessao.EncerradoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapSessaoResponse(sessao));
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private async Task<HashSet<int>> ColetarMenuIdsElegiveis(int empresaId, int? empresaMaeId)
        {
            var ids = new HashSet<int>();

            var proprios = await _context.Menus
                .Where(m => m.EmpresaId == empresaId && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync();
            ids.UnionWith(proprios);

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

        private static SessaoResponseDTO MapSessaoResponse(SessaoMesa sessao) => new()
        {
            Token      = sessao.Token,
            NumeroMesa = sessao.NumeroMesa,
            Status     = sessao.Status,
            AbertoEm   = sessao.AbertoEm,
            EncerradoEm = sessao.EncerradoEm,
            Pedidos    = sessao.Pedidos.Select(MapPedidoResponse).ToList()
        };

        private static PedidoResponseDTO MapPedidoResponse(Pedido pedido) => new()
        {
            Id        = pedido.Id,
            Status    = pedido.Status,
            CreatedAt = pedido.CreatedAt,
            Itens     = pedido.Itens.Select(i => new PedidoItemResponseDTO
            {
                NomeItem      = i.NomeItem,
                PrecoUnitario = i.PrecoUnitario,
                Quantidade    = i.Quantidade,
                Observacao    = i.Observacao
            }).ToList()
        };
    }
}
