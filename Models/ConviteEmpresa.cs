using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models
{
    public class ConviteEmpresa
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Email { get; set; } = string.Empty;

        /// <summary>Token único enviado no link do convite.</summary>
        public string Token { get; set; } = string.Empty;

        public StatusConvite Status { get; set; } = StatusConvite.Pendente;
        public DateTime ExpiresAt { get; set; }

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        /// <summary>Cargo que o convidado receberá ao aceitar.</summary>
        public int CargoId { get; set; }
        public Cargo Cargo { get; set; } = null!;
    }
}
