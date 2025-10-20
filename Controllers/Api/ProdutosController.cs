using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Data;
using HortalisCSharp.Models;

namespace HortalisCSharp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProdutosController(AppDbContext db) => _db = db;

        // GET /api/produtos?query=ala
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? query)
        {
            var q = (query ?? string.Empty).Trim();
            var produtos = await _db.Produtos
                .Where(p => q == "" || p.Nome.ToLower().Contains(q.ToLower()))
                .OrderBy(p => p.Nome)
                .Take(20)
                .Select(p => new { id = p.Id, nome = p.Nome })
                .ToListAsync();
            return Ok(produtos);
        }

        // POST /api/produtos  { nome: "alface" }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProdutoCreateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Nome)) return BadRequest();
            var nome = model.Nome.Trim();

            var existing = await _db.Produtos.FirstOrDefaultAsync(p => p.Nome.ToLower() == nome.ToLower());
            if (existing != null) return Conflict(new { message = "Produto já existe", id = existing.Id, nome = existing.Nome });

            var produto = new Produto { Nome = nome };
            _db.Produtos.Add(produto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = produto.Id }, new { id = produto.Id, nome = produto.Nome });
        }

        public class ProdutoCreateModel { public string Nome { get; set; } = null!; }
    }
}