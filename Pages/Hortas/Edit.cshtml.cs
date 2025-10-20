using System.Security.Claims;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using HortalisCSharp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HortalisCSharp.Pages.Hortas
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IHortaAuthorizationService _auth;

        public EditModel(AppDbContext db, IHortaAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        [BindProperty]
        public Horta Input { get; set; } = new();

        // campo usado pela UI
        [BindProperty]
        public string? ProdutoNomes { get; set; }

        public async Task<IActionResult> OnGet(int id)
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas
                .Include(x => x.HortaProdutos)
                    .ThenInclude(hp => hp.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            Input = h;

            // preenche ProdutoNomes apenas a partir das associações normalizadas
            if (h.HortaProdutos != null && h.HortaProdutos.Any())
            {
                ProdutoNomes = string.Join(",", h.HortaProdutos.Select(hp => hp.Produto?.Nome ?? ""));
            }
            else
            {
                ProdutoNomes = string.Empty;
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas
                .Include(x => x.HortaProdutos)
                .FirstOrDefaultAsync(x => x.Id == Input.Id);
            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            if (!ModelState.IsValid) return Page();

            h.Nome = Input.Nome;
            h.Latitude = Input.Latitude;
            h.Longitude = Input.Longitude;
            h.Descricao = Input.Descricao;
            h.Foto = Input.Foto;
            h.Telefone = Input.Telefone;

            var nomes = (ProdutoNomes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var atuais = _db.HortaProdutos.Where(hp => hp.HortaId == h.Id);
            _db.HortaProdutos.RemoveRange(atuais);

            foreach (var nome in nomes)
            {
                var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Nome.ToLower() == nome.ToLower());
                if (produto is null)
                {
                    produto = new Produto { Nome = nome };
                    _db.Produtos.Add(produto);
                    await _db.SaveChangesAsync();
                }

                _db.HortaProdutos.Add(new HortaProduto { HortaId = h.Id, ProdutoId = produto.Id });
            }

            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        private async Task<Usuario?> ObterUsuarioAtualAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}