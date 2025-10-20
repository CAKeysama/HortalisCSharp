using System.Security.Claims;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using HortalisCSharp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HortalisCSharp.Pages.Hortas
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IHortaAuthorizationService _auth;

        public CreateModel(AppDbContext db, IHortaAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        [BindProperty]
        public Horta Input { get; set; } = new();

        // campo usado para UI (lista de nomes separados por vírgula)
        [BindProperty]
        public string? ProdutoNomes { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null || !_auth.CanCreate(user)) return Forbid();
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null || !_auth.CanCreate(user)) return Forbid();

            if (!ModelState.IsValid) return Page();

            Input.CriadoEm = DateTime.UtcNow;
            Input.UsuarioId = user.Id;

            _db.Hortas.Add(Input);
            await _db.SaveChangesAsync();

            var nomes = (ProdutoNomes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var nome in nomes)
            {
                var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Nome.ToLower() == nome.ToLower());
                if (produto is null)
                {
                    produto = new Produto { Nome = nome };
                    _db.Produtos.Add(produto);
                    await _db.SaveChangesAsync();
                }

                var hpExists = await _db.HortaProdutos.AnyAsync(hp => hp.HortaId == Input.Id && hp.ProdutoId == produto.Id);
                if (!hpExists)
                {
                    _db.HortaProdutos.Add(new HortaProduto { HortaId = Input.Id, ProdutoId = produto.Id });
                }
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