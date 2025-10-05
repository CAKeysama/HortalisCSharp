using System.ComponentModel.DataAnnotations;

namespace HortalisCSharp.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Nome { get; set; } = null!;

        [Required, EmailAddress, MaxLength(160)]
        public string Email { get; set; } = null!;

        [Required]
        public string SenhaHash { get; set; } = null!;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}