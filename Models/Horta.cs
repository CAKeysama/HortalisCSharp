using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HortalisCSharp.Models
{
    public class Horta
    {
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Nome { get; set; } = null!;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [MaxLength(2000)]
        public string? Descricao { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Foto { get; set; }

        [MaxLength(40)]
        public string? Telefone { get; set; }

        // Dono/criador da horta
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Navegação para produtos normalizados
        public ICollection<HortaProduto> HortaProdutos { get; set; } = new List<HortaProduto>();
    }
}