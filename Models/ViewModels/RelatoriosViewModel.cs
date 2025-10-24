using System;
using System.Collections.Generic;

namespace HortalisCSharp.Models.ViewModels
{
    public class RelatoriosViewModel
    {
        public int TotalHortas { get; set; }
        public int TotalProdutos { get; set; }
        public int TotalUsuarios { get; set; }

        // M�tricas detalhadas
        public double MediaProdutosPorHorta { get; set; }
        public int ProdutosSemHorta { get; set; }

        public List<ProductStat> TopProdutos { get; set; } = new();
        public List<UserStat> TopUsuarios { get; set; } = new();
        public List<HortaSummary> HortasRecentes { get; set; } = new();

        // Nova propriedade: lista edit�vel de usu�rios
        public List<UserItem> Usuarios { get; set; } = new();

        // Nova propriedade: indica��es agregadas por �rea (usada em Relat�rios)
        public List<IndicacaoArea> IndicacoesPorArea { get; set; } = new();

        public class ProductStat
        {
            public string Nome { get; set; } = null!;
            public int Count { get; set; }
        }

        public class UserStat
        {
            public int? UsuarioId { get; set; }
            public string Nome { get; set; } = null!;
            public int Count { get; set; }
        }

        // Item para edi��o r�pida de papel
        public class UserItem
        {
            public int UsuarioId { get; set; }
            public string Nome { get; set; } = null!;
            public string Email { get; set; } = null!;
            public int Papel { get; set; }
        }

        public class HortaSummary
        {
            public int Id { get; set; }
            public string Nome { get; set; } = null!;
            public string? UsuarioNome { get; set; }
            public DateTime CriadoEm { get; set; }
            public int ProdutoCount { get; set; }

            // Nova propriedade: data da �ltima altera��o (nullable se n�o existir)
            public DateTime? UltimaAlteracao { get; set; }
        }

        // Representa uma �rea com quantidade de indica��es e coordenadas (para mapa)
        public class IndicacaoArea
        {
            public string AreaNome { get; set; } = null!;
            public double? Latitude { get; set; }   // agora nullable
            public double? Longitude { get; set; }  // agora nullable
            public int Count { get; set; }
        }
    }
}