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