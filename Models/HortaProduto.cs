namespace HortalisCSharp.Models
{
    public class HortaProduto
    {
        public int HortaId { get; set; }
        public Horta Horta { get; set; } = null!;

        public int ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;
    }
}