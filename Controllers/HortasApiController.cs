using HortalisCSharp.Data;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HortaMapDto>>> Get()
        {
            var list = await _db.Hortas
                .AsNoTracking()
                .Select(h => new HortaMapDto(h.Id, h.Nome, h.Latitude, h.Longitude, h.Produtos))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<HortaDetailsDto>> GetById([Range(1, int.MaxValue)] int id)
        {
            var h = await _db.Hortas
                .AsNoTracking()
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (h == null) return NotFound();

            var dto = new HortaDetailsDto(
                h.Id,
                h.Nome,
                h.Latitude,
                h.Longitude,
                h.Produtos,
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