namespace MenuBuilderBack.Models
{
    public class PedidoItem
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        /// <summary>Referência histórica ao item do menu — pode ter sido alterado/removido depois.</summary>
        public int MenuItemId { get; set; }

        /// <summary>Snapshot do nome no momento do pedido.</summary>
        public string NomeItem { get; set; } = string.Empty;

        /// <summary>Snapshot do preço no momento do pedido (com override aplicado, se houver).</summary>
        public decimal PrecoUnitario { get; set; }

        public int Quantidade { get; set; }

        public string? Observacao { get; set; }
    }
}
