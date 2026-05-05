using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models
{
    public class SessaoMesa
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        /// <summary>Token público opaco — evita enumeração de sessões por ID sequencial.</summary>
        public Guid Token { get; set; } = Guid.NewGuid();

        public string NumeroMesa { get; set; } = string.Empty;

        public StatusSessao Status { get; set; } = StatusSessao.Aberta;

        public DateTime AbertoEm { get; set; } = DateTime.UtcNow;
        public DateTime? EncerradoEm { get; set; }

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
