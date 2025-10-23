using HortalisCSharp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HortalisCSharp.Controllers
{
    [ApiController]
    [Route("api/hortas")]
    public class HortasApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HortasApiController(AppDbContext db)
        {
            _db = db;
        }

        public sealed record HortaMapDto(
            int Id,
            string Nome,
            double Latitude,
            double Longitude,
            string? Produtos
        );

        public sealed record HortaDetailsDto(
            int Id,
            string Nome,
            double Latitude,
            double Longitude,
            string? Produtos,
            string? Descricao,
            string? Foto,
            string? Telefone,
            DateTime CriadoEm,
            string? Dono
        );

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HortaMapDto>>> Get()
        {
            var list = await _db.Hortas
                .AsNoTracking()
                .Select(h => new HortaMapDto(
                    h.Id,
                    h.Nome,
                    h.Latitude,
                    h.Longitude,
                    // Corrigido: obtendo os nomes dos produtos relacionados à horta
                    string.Join(", ", h.HortaProdutos.Select(hp => hp.Produto.Nome))
                ))
                .ToListAsync();

            return Ok(list);
        }

        // Permite acesso anônimo aos detalhes de uma horta (para uso público no mapa)
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<HortaDetailsDto>> GetById([Range(1, int.MaxValue)] int id)
        {
            var h = await _db.Hortas
                .AsNoTracking()
                .Include(x => x.Usuario)
                .Include(x => x.HortaProdutos)
                    .ThenInclude(hp => hp.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (h == null) return NotFound();

            var dto = new HortaDetailsDto(
                h.Id,
                h.Nome,
                h.Latitude,
                h.Longitude,
                // Corrigido: obtendo os nomes dos produtos relacionados à horta
                string.Join(", ", h.HortaProdutos.Select(hp => hp.Produto.Nome)),
                h.Descricao,
                h.Foto,
                h.Telefone,
                h.CriadoEm,
                h.Usuario?.Nome
            );

            return Ok(dto);
        }
    }
}