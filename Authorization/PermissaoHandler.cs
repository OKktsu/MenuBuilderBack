using MenuBuilderBack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Authorization
{
    public class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
    {
        private readonly AppDbContext _context;

        public PermissaoHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissaoRequirement requirement)
        {
            var user = context.User;

            // Dono tem acesso total — não precisa checar cargo
            var isOwner = user.FindFirst("isOwner")?.Value == "true";
            if (isOwner)
            {
                context.Succeed(requirement);
                return;
            }

            // Funcionário precisa ter cargo com a permissão necessária
            var cargoIdStr = user.FindFirst("cargoId")?.Value;
            if (!int.TryParse(cargoIdStr, out var cargoId) || cargoId <= 0)
                return; // falha — sem cargo definido

            var temPermissao = await _context.CargoPermissoes
                .AnyAsync(cp => cp.CargoId == cargoId
                             && cp.Permissao == requirement.Permissao);

            if (temPermissao)
                context.Succeed(requirement);
        }
    }
}
