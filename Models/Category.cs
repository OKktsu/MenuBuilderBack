using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuBuilderBack.Models.Base;

namespace MenuBuilderBack.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }              // posição do drag and drop

        // Chave estrangeira
        public int MenuId { get; set; }
        public Menu Menu { get; set; } = null!;

        // Relacionamento
        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
    }
}