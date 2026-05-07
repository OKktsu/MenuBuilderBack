using FluentAssertions;
using MenuBuilderBack.Helpers;

namespace MenuBuilderBack.Tests.Unit
{
    /// <summary>
    /// Testes de unidade para o SlugHelper.
    /// Não precisam de banco, não precisam de HTTP — testam lógica pura.
    /// </summary>
    public class SlugHelperTests
    {
        // ── Gerar ──────────────────────────────────────────────────────────────

        [Fact]
        public void Gerar_DeveLowercasearTexto()
        {
            var slug = SlugHelper.Gerar("Pizzaria Central");
            slug.Should().Be("pizzaria-central");
        }

        [Fact]
        public void Gerar_DeveRemoverAcentos()
        {
            var slug = SlugHelper.Gerar("Café São João");
            slug.Should().Be("cafe-sao-joao");
        }

        [Fact]
        public void Gerar_DeveSubstituirEspacosPorHifens()
        {
            var slug = SlugHelper.Gerar("Burguer King");
            slug.Should().Be("burguer-king");
        }

        [Fact]
        public void Gerar_DeveRemoverCaracteresEspeciais()
        {
            var slug = SlugHelper.Gerar("Churrasco & Cia!");
            slug.Should().Be("churrasco-cia");
        }

        [Fact]
        public void Gerar_NaoDeveComecouOuTerminarComHifen()
        {
            var slug = SlugHelper.Gerar("  Restaurante ABC  ");
            slug.Should().NotStartWith("-").And.NotEndWith("-");
        }

        [Fact]
        public void Gerar_DeveAgruparEspacosConsecutivos()
        {
            var slug = SlugHelper.Gerar("A   B");
            slug.Should().Be("a-b");
        }

        [Theory]
        [InlineData("Sushi do Zé", "sushi-do-ze")]
        [InlineData("Açaí & Granola", "acai-granola")]
        [InlineData("123 Burguer", "123-burguer")]
        public void Gerar_CasosVariados(string nome, string esperado)
        {
            SlugHelper.Gerar(nome).Should().Be(esperado);
        }
    }
}
