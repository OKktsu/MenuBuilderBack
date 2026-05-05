using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        public int SessaoMesaId { get; set; }
        public SessaoMesa SessaoMesa { get; set; } = null!;

        public StatusPedido Status { get; set; } = StatusPedido.Recebido;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
    }
}
