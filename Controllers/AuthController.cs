using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MenuBuilderBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, UserManager<User> userManager, IConfiguration config)
        {
            _config = config;
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ResgisterDTO model)
        {
            Console.WriteLine($"Tentando registrar usuário: {model.Email}"); // Log para depuração

            // 1. Verifica se o usuário já existe
            var userExists = await  _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "Este e-mail já está sendo usado." });

            // 2. Cria a instância do seu usuário customizado
            var user = new User
            {
                Email = model.Email,
                UserName = model.Email, // O Identity usa UserName como chave principal
                NomeCompleto = model.NomeCompleto,
                SecurityStamp = Guid.NewGuid().ToString() // Garante a integridade do token
            };

            // 3. Tenta salvar no Supabase
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuário criado com sucesso no MenuBuilder!" });
        }
    
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);

            if (user == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            // Use o Identity para verificar a senha, não o BCrypt diretamente
            var passwordValid = await _userManager.CheckPasswordAsync(user, login.Password);

            if (!passwordValid)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var token = GenerateJwtToken(user);

            return Ok(new { 
                token = token,
                user = new { user.Id, user.NomeCompleto, user.Email }
            });
        }
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        // Lendo a chave secreta do appsettings.json
        var secretKey = _config["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

        var key = Encoding.ASCII.GetBytes(secretKey);

       var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.NomeCompleto ?? "")}),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    }
}