using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HortalisCSharp.Models
{
    public class Produto
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Nome { get; set; } = null!;

        public ICollection<HortaProduto> HortaProdutos { get; set; } = new List<HortaProduto>();
    }
}