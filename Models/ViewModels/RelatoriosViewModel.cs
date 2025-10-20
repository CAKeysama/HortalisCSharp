namespace HortalisCSharp.Models.ViewModels
{
    public class RelatoriosViewModel
    {
        public int TotalHortas { get; set; }
        public int TotalProdutos { get; set; }
        public int TotalUsuarios { get; set; }

        // Métricas detalhadas
        public double MediaProdutosPorHorta { get; set; }
        public int ProdutosSemHorta { get; set; }

        public List<ProductStat> TopProdutos { get; set; } = new();
        public List<UserStat> TopUsuarios { get; set; } = new();
        public List<HortaSummary> HortasRecentes { get; set; } = new();

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

        public class HortaSummary
        {
            public int Id { get; set; }
            public string Nome { get; set; } = null!;
            public string? UsuarioNome { get; set; }
            public DateTime CriadoEm { get; set; }
            public int ProdutoCount { get; set; }
        }
    }
}