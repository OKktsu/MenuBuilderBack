using MenuBuilderBack.Data;
using MenuBuilderBack.Helpers;
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
    public class EmpresaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPhotoService _photoService;
        private readonly ILogger<EmpresaController> _logger;

        public EmpresaController(AppDbContext context, IPhotoService photoService, ILogger<EmpresaController> logger)
        {
            _context = context;
            _photoService = photoService;
            _logger = logger;
        }

        /// <summary>Retorna os dados da empresa do usuário autenticado.</summary>
        [HttpGet("minha")]
        public async Task<IActionResult> GetMinha()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var empresa = await _context.Empresas.FindAsync(empresaId);
            if (empresa == null) return NotFound(new { message = "Empresa não encontrada." });

            return Ok(MapToResponseDTO(empresa));
        }

        /// <summary>Atualiza nome e/ou logo da empresa. Exclusivo para donos.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] EmpresaRequestDTO dto)
        {
            // Apenas o dono pode editar dados da empresa
            var isOwner = User.FindFirst("isOwner")?.Value == "true";
            if (!isOwner) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var empresa = await _context.Empresas.FindAsync(empresaId);
            if (empresa == null) return NotFound(new { message = "Empresa não encontrada." });

            empresa.Nome      = dto.Nome;
            empresa.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.LogoUrl))
            {
                if (dto.LogoUrl.StartsWith("data:image"))
                {
                    var upload = await _photoService.AddPhotoAsync(dto.LogoUrl);
                    if (upload.Error != null)
                        return BadRequest(new { message = upload.Error.Message });
                    empresa.LogoUrl = upload.SecureUrl.AbsoluteUri;
                }
                else
                {
                    empresa.LogoUrl = dto.LogoUrl;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Empresa {EmpresaId} atualizada.", empresa.Id);

            return Ok(MapToResponseDTO(empresa));
        }

        /// <summary>Atualiza o slug público da empresa. Exclusivo para donos.</summary>
        [HttpPut("slug")]
        public async Task<IActionResult> UpdateSlug([FromBody] SlugUpdateDTO dto)
        {
            var isOwner = User.FindFirst("isOwner")?.Value == "true";
            if (!isOwner) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            var slugEmUso = await _context.Empresas
                .AnyAsync(e => e.Slug == dto.Slug && e.Id != empresaId);
            if (slugEmUso)
                return Conflict(new { message = "Este slug já está em uso por outra empresa." });

            var empresa = await _context.Empresas.FindAsync(empresaId);
            if (empresa == null) return NotFound();

            empresa.Slug = dto.Slug;
            empresa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Slug da empresa {EmpresaId} atualizado para '{Slug}'.", empresa.Id, empresa.Slug);
            return Ok(MapToResponseDTO(empresa));
        }

        /// <summary>Cria uma empresa filha vinculada à empresa do dono autenticado.</summary>
        [HttpPost("filha")]
        public async Task<IActionResult> CriarFilha([FromBody] EmpresaFilhaRequestDTO dto)
        {
            var isOwner = User.FindFirst("isOwner")?.Value == "true";
            if (!isOwner) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var empresaMaeId = ObterEmpresaId();
            if (empresaMaeId == null) return Unauthorized();

            var filha = new Empresa
            {
                Nome          = dto.Nome,
                CodigoConvite = GerarCodigoConvite(),
                Slug          = await SlugHelper.GerarUnico(dto.Nome, _context),
                EmpresaMaeId  = empresaMaeId.Value
            };

            if (!string.IsNullOrEmpty(dto.LogoUrl) && dto.LogoUrl.StartsWith("data:image"))
            {
                var upload = await _photoService.AddPhotoAsync(dto.LogoUrl);
                if (upload.Error != null)
                    return BadRequest(new { message = upload.Error.Message });
                filha.LogoUrl = upload.SecureUrl.AbsoluteUri;
            }
            else if (!string.IsNullOrEmpty(dto.LogoUrl))
            {
                filha.LogoUrl = dto.LogoUrl;
            }

            _context.Empresas.Add(filha);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Empresa filha {FilhaId} criada pela empresa mãe {MaeId}.", filha.Id, empresaMaeId);
            return CreatedAtAction(nameof(GetMinha), MapToResponseDTO(filha));
        }

        /// <summary>Retorna a árvore hierárquica completa da empresa do usuário autenticado.</summary>
        [HttpGet("arvore")]
        public async Task<IActionResult> GetArvore()
        {
            var empresaId = ObterEmpresaId();
            if (empresaId == null) return Unauthorized();

            // Carrega todas as empresas do grupo (raiz + filhas em todos os níveis)
            var todasEmpresas = await _context.Empresas
                .Where(e => e.IsActive)
                .Select(e => new { e.Id, e.Nome, e.Slug, e.LogoUrl, e.EmpresaMaeId })
                .ToListAsync();

            // Encontra a raiz da hierarquia para o usuário atual
            var raizId = EncontrarRaiz(empresaId.Value, todasEmpresas.ToDictionary(e => e.Id, e => e.EmpresaMaeId));

            EmpresaArvoreDTO Construir(int id)
            {
                var e = todasEmpresas.First(x => x.Id == id);
                return new EmpresaArvoreDTO
                {
                    Id      = e.Id,
                    Nome    = e.Nome,
                    Slug    = e.Slug,
                    LogoUrl = e.LogoUrl,
                    Filhas  = todasEmpresas
                        .Where(x => x.EmpresaMaeId == id)
                        .Select(x => Construir(x.Id))
                        .ToList()
                };
            }

            return Ok(Construir(raizId));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int? ObterEmpresaId()
        {
            var value = User.FindFirst("empresaId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static EmpresaResponseDTO MapToResponseDTO(Empresa e) => new()
        {
            Id            = e.Id,
            Nome          = e.Nome,
            LogoUrl       = e.LogoUrl,
            CodigoConvite = e.CodigoConvite,
            Slug          = e.Slug,
            EmpresaMaeId  = e.EmpresaMaeId,
            CreatedAt     = e.CreatedAt
        };

        private static int EncontrarRaiz(int id, Dictionary<int, int?> maeMap)
        {
            var atual = id;
            while (maeMap.TryGetValue(atual, out var maeId) && maeId.HasValue)
                atual = maeId.Value;
            return atual;
        }

        private static string GerarCodigoConvite()
            => Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}
