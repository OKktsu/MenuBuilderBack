using FluentAssertions;
using MenuBuilderBack.Data;
using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Dtos;
using MenuBuilderBack.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MenuBuilderBack.Tests.Integration
{
    /// <summary>
    /// Testes de integração para o cardápio público e fluxo de pedidos.
    /// A aplicação sobe completa em memória — os testes fazem chamadas HTTP reais contra ela.
    /// </summary>
    public class PublicMenuTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly TestWebApplicationFactory _factory;

        // A API serializa enums como string (JsonStringEnumConverter no Program.cs)
        // — usamos as mesmas opções ao desserializar as respostas nos testes.
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public PublicMenuTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ── Cardápio ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetCardapio_SlugInexistente_Retorna404()
        {
            var response = await _client.GetAsync("/api/public/restaurante-que-nao-existe");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetCardapio_EmpresaAtiva_RetornaCardapio()
        {
            _factory.SeedDatabase(db => SeedEmpresaComMenu(db, "restaurante-teste-1"));

            var response = await _client.GetAsync("/api/public/restaurante-teste-1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PublicMenuResponseDTO>(JsonOpts);
            body.Should().NotBeNull();
            body!.RestaurantName.Should().Be("Restaurante Teste");
            body.Menus.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCardapio_EmpresaInativa_Retorna404()
        {
            _factory.SeedDatabase(db => SeedEmpresaComMenu(db, "restaurante-inativo", ativo: false));

            var response = await _client.GetAsync("/api/public/restaurante-inativo");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ── Sessão de Mesa ────────────────────────────────────────────────────

        [Fact]
        public async Task AbrirSessao_SlugValido_RetornaTokenESessaoAberta()
        {
            _factory.SeedDatabase(db => SeedEmpresaComMenu(db, "restaurante-sessao-1"));

            var response = await _client.PostAsJsonAsync(
                "/api/public/restaurante-sessao-1/sessao",
                new { numeroMesa = "5" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<SessaoResponseDTO>(JsonOpts);
            body.Should().NotBeNull();
            body!.Token.Should().NotBeEmpty();
            body.NumeroMesa.Should().Be("5");
            body.Status.Should().Be(Models.Enums.StatusSessao.Aberta);
        }

        [Fact]
        public async Task FazerPedido_SessaoAberta_CriaOrdemComSnapshot()
        {
            int menuItemId = 0;
            _factory.SeedDatabase(db =>
            {
                var empresa = SeedEmpresaComMenu(db, "restaurante-pedido-1");
                menuItemId = empresa.Menus.First().Categories.First().Items.First().Id;
            });

            // Abre sessão
            var sessaoResp = await _client.PostAsJsonAsync(
                "/api/public/restaurante-pedido-1/sessao",
                new { numeroMesa = "3" });
            var sessao = await sessaoResp.Content.ReadFromJsonAsync<SessaoResponseDTO>(JsonOpts);

            // Faz pedido
            var pedidoResp = await _client.PostAsJsonAsync(
                $"/api/public/restaurante-pedido-1/sessao/{sessao!.Token}/pedido",
                new { itens = new[] { new { menuItemId, quantidade = 2 } } });

            pedidoResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var pedido = await pedidoResp.Content.ReadFromJsonAsync<PedidoResponseDTO>(JsonOpts);
            pedido!.Itens.Should().HaveCount(1);
            pedido.Itens[0].Quantidade.Should().Be(2);
            pedido.Itens[0].NomeItem.Should().Be("X-Burguer");
            pedido.Itens[0].PrecoUnitario.Should().Be(25.90m);
        }

        [Fact]
        public async Task EncerrarSessao_SessaoAberta_RetornaEncerrada()
        {
            _factory.SeedDatabase(db => SeedEmpresaComMenu(db, "restaurante-encerrar-1"));

            var sessaoResp = await _client.PostAsJsonAsync(
                "/api/public/restaurante-encerrar-1/sessao",
                new { numeroMesa = "1" });
            var sessao = await sessaoResp.Content.ReadFromJsonAsync<SessaoResponseDTO>(JsonOpts);

            var response = await _client.PutAsync(
                $"/api/public/restaurante-encerrar-1/sessao/{sessao!.Token}/encerrar",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<SessaoResponseDTO>(JsonOpts);
            body!.Status.Should().Be(Models.Enums.StatusSessao.Encerrada);
            body.EncerradoEm.Should().NotBeNull();
        }

        [Fact]
        public async Task FazerPedido_SessaoEncerrada_Retorna409()
        {
            _factory.SeedDatabase(db => SeedEmpresaComMenu(db, "restaurante-sessao-encerrada"));

            var sessaoResp = await _client.PostAsJsonAsync(
                "/api/public/restaurante-sessao-encerrada/sessao",
                new { numeroMesa = "2" });
            var sessao = await sessaoResp.Content.ReadFromJsonAsync<SessaoResponseDTO>(JsonOpts);

            await _client.PutAsync(
                $"/api/public/restaurante-sessao-encerrada/sessao/{sessao!.Token}/encerrar", null);

            var pedidoResp = await _client.PostAsJsonAsync(
                $"/api/public/restaurante-sessao-encerrada/sessao/{sessao.Token}/pedido",
                new { itens = new[] { new { menuItemId = 1, quantidade = 1 } } });

            pedidoResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        // ── Helpers de seed ───────────────────────────────────────────────────

        private static Empresa SeedEmpresaComMenu(AppDbContext db, string slug, bool ativo = true)
        {
            var item = new MenuItem
            {
                Name      = "X-Burguer",
                Price     = 25.90m,
                IsActive  = true,
                EmpresaId = 0, // será atribuído pelo EF ao salvar
                Tags      = []
            };

            var categoria = new Category
            {
                Name     = "Lanches",
                Order    = 1,
                IsActive = true,
                Items    = [item]
            };

            var menu = new Menu
            {
                RestaurantName = "Menu Principal",
                IsActive       = true,
                Categories     = [categoria]
            };

            var empresa = new Empresa
            {
                Nome          = "Restaurante Teste",
                Slug          = slug,
                CodigoConvite = Guid.NewGuid().ToString("N")[..8],
                IsActive      = ativo,
                Menus         = [menu]
            };

            // EF cuida das FKs em cascata ao adicionar a empresa raiz
            item.EmpresaId = 0; // será resolvido pelo EF
            db.Empresas.Add(empresa);
            db.SaveChanges();

            // Corrige EmpresaId dos itens (InMemory não tem FK enforcement)
            foreach (var i in db.MenuItems)
                if (i.EmpresaId == 0) { i.EmpresaId = empresa.Id; }
            db.SaveChanges();

            return db.Empresas
                .Where(e => e.Slug == slug)
                .Select(e => e)
                .First();
        }
    }
}
