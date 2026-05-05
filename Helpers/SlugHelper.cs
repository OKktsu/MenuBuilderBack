using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MenuBuilderBack.Data;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Helpers
{
    public static class SlugHelper
    {
        public static string Gerar(string nome)
        {
            var normalizado = nome.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var semAcentos = sb.ToString().Normalize(NormalizationForm.FormC);
            return Regex.Replace(semAcentos.ToLower(), @"[^a-z0-9]+", "-").Trim('-');
        }

        public static async Task<string> GerarUnico(string nome, AppDbContext context, int? ignorarId = null)
        {
            var baseSlug = Gerar(nome);
            var slug = baseSlug;
            var contador = 2;

            while (await context.Empresas.AnyAsync(e => e.Slug == slug && e.Id != ignorarId))
                slug = $"{baseSlug}-{contador++}";

            return slug;
        }
    }
}
