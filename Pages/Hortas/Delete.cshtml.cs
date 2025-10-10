using System.Security.Claims;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using HortalisCSharp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HortalisCSharp.Pages.Hortas
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IHortaAuthorizationService _auth;

        public DeleteModel(AppDbContext db, IHortaAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        [BindProperty]
        public int Id { get; set; }

        public Horta? Horta { get; private set; }

        public async Task<IActionResult> OnGet(int id)
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas.Include(x => x.Usuario).FirstOrDefaultAsync(x => x.Id == id);
            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            Horta = h;
            Id = id;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null) return Forbid();

            var h = await _db.Hortas.FirstOrDefaultAsync(x => x.Id == Id);
            if (h is null) return NotFound();

            if (!_auth.CanManage(user, h)) return Forbid();

            _db.Hortas.Remove(h);
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