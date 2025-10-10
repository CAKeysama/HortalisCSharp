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

        public async Task<IActionResult> OnGet(int id)
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas.FirstOrDefaultAsync(x => x.Id == id);
            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            Input = h;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas.FirstOrDefaultAsync(x => x.Id == Input.Id);
            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            if (!ModelState.IsValid) return Page();

            // Atualiza campos editáveis
            h.Nome = Input.Nome;
            h.Latitude = Input.Latitude;
            h.Longitude = Input.Longitude;
            h.Descricao = Input.Descricao;
            h.Produtos = Input.Produtos;
            h.Foto = Input.Foto;
            h.Telefone = Input.Telefone;

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