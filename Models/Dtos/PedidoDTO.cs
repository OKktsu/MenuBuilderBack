using System.ComponentModel.DataAnnotations;
using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models.Dtos
{
    // ── Requests (cliente público) ────────────────────────────────────────────

    public class AbrirSessaoRequestDTO
    {
        [Required, MaxLength(20)]
        public string NumeroMesa { get; set; } = string.Empty;
    }

    public class NovoPedidoRequestDTO
    {
        [Required, MinLength(1)]
        public List<PedidoItemRequestDTO> Itens { get; set; } = [];
    }

    public class PedidoItemRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int MenuItemId { get; set; }

        [Range(1, 50)]
        public int Quantidade { get; set; }

        [MaxLength(300)]
        public string? Observacao { get; set; }
    }

    // ── Responses (cliente público) ───────────────────────────────────────────

    public class SessaoResponseDTO
    {
        public Guid Token { get; set; }
        public string NumeroMesa { get; set; } = string.Empty;
        public StatusSessao Status { get; set; }
        public DateTime AbertoEm { get; set; }
        public DateTime? EncerradoEm { get; set; }
        public List<PedidoResponseDTO> Pedidos { get; set; } = [];
        public decimal Total => Pedidos.Sum(p => p.Subtotal);
    }

    public class PedidoResponseDTO
    {
        public int Id { get; set; }
        public StatusPedido Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PedidoItemResponseDTO> Itens { get; set; } = [];
        public decimal Subtotal => Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
    }

    public class PedidoItemResponseDTO
    {
        public string NomeItem { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public string? Observacao { get; set; }
    }

    // ── Admin / Cozinha ───────────────────────────────────────────────────────

    public class SessaoAdminDTO
    {
        public int Id { get; set; }
        public string NumeroMesa { get; set; } = string.Empty;
        public StatusSessao Status { get; set; }
        public DateTime AbertoEm { get; set; }
        public DateTime? EncerradoEm { get; set; }
        public int TotalPedidos { get; set; }
        public decimal Total { get; set; }
    }

    public class PedidoAdminDTO
    {
        public int Id { get; set; }
        public string NumeroMesa { get; set; } = string.Empty;
        public StatusPedido Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PedidoItemResponseDTO> Itens { get; set; } = [];
        public decimal Subtotal => Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
    }

    public class AtualizarStatusPedidoDTO
    {
        [Required]
        public StatusPedido Status { get; set; }
    }
}
