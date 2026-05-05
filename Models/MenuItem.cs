using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuBuilderBack.Models.Base;

namespace MenuBuilderBack.Models
{
    public class MenuItem : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
        public string? Ingredients { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public List<string> Tags { get; set; } = new();

        // Isolamento por empresa
        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}