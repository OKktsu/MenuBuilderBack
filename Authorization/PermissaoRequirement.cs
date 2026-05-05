using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace MenuBuilderBack.Authorization
{
    /// <summary>
    /// Requisito de autorização baseado em permissão de cargo.
    /// Donos (IsOwner = true) passam automaticamente em qualquer permissão.
    /// </summary>
    public class PermissaoRequirement : IAuthorizationRequirement
    {
        public Permissao Permissao { get; }
        public PermissaoRequirement(Permissao permissao) => Permissao = permissao;
    }
}
