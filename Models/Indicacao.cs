using System;
using System.ComponentModel.DataAnnotations;

namespace HortalisCSharp.Models
{
    public class Indicacao
    {
        public int Id { get; set; }

        [MaxLength(160)]
        public string? Nome { get; set; }

        // Removido: TipoHorta

        [MaxLength(200)]
        public string? Email { get; set; }

        // Removido: AlimentosCultivados

        // Removido: NomeHorta

        // Removido: Localizacao

        [MaxLength(120)]
        public string? AreaNome { get; set; }

        // Latitude/Longitude podem ser nulos se usuário não informar
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Removido: Descricao

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}