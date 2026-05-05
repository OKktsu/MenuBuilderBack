using MenuBuilderBack.Data;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MenuBuilderBack.Controllers
{
    /// <summary>
    /// Painel da cozinha — visualiza sessões abertas e atualiza status dos pedidos.
    /// Requer autenticação; qualquer funcionário da empresa tem acesso de leitura.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CozinhaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CozinhaController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Lista todas as sessões abertas da empresa, com contagem e total.</summary>
        [HttpGet("sessoes")]
        public async Task<IActionResult> GetSessoes([FromQuery] StatusSessao status = StatusSessao.Aberta)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var sessoes = await _context.SessoesMesa
                .Where(s => s.EmpresaId == empresaId && s.Status == status)
                .OrderBy(s => s.AbertoEm)
                .Select(s => new SessaoAdminDTO
                {
                    Id           = s.Id,
                    NumeroMesa   = s.NumeroMesa,
                    Status       = s.Status,
                    AbertoEm     = s.AbertoEm,
                    EncerradoEm  = s.EncerradoEm,
                    TotalPedidos = s.Pedidos.Count,
                    Total        = s.Pedidos
                        .SelectMany(p => p.Itens)
                        .Sum(i => i.PrecoUnitario * i.Quantidade)
                })
                .ToListAsync();

            return Ok(sessoes);
        }

        /// <summary>Lista pedidos da empresa filtrados por status. Padrão: Recebido + EmPreparo.</summary>
        [HttpGet("pedidos")]
        public async Task<IActionResult> GetPedidos([FromQuery] StatusPedido? status = null)
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var query = _context.Pedidos
                .Where(p => p.SessaoMesa.EmpresaId == empresaId);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);
            else
                query = query.Where(p => p.Status == StatusPedido.Recebido || p.Status == StatusPedido.EmPreparo);

            var pedidos = await query
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PedidoAdminDTO
                {
                    Id         = p.Id,
                    NumeroMesa = p.SessaoMesa.NumeroMesa,
                    Status     = p.Status,
                    CreatedAt  = p.CreatedAt,
                    Itens      = p.Itens.Select(i => new PedidoItemResponseDTO
                    {
                        NomeItem      = i.NomeItem,
                        PrecoUnitario = i.PrecoUnitario,
                        Quantidade    = i.Quantidade,
                        Observacao    = i.Observacao
                    }).ToList()
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        /// <summary>Atualiza o status de um pedido.</summary>
        [HttpPut("pedidos/{id}/status")]
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusPedidoDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var pedido = await _context.Pedidos
                .Where(p => p.Id == id && p.SessaoMesa.EmpresaId == empresaId)
                .FirstOrDefaultAsync();

            if (pedido == null) return NotFound();

            pedido.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { pedido.Id, pedido.Status });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirstValue("empresaId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
