using System.Security.Claims;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using HortalisCSharp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HortalisCSharp.Pages.Hortas
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IHortaAuthorizationService _auth;

        public IndexModel(AppDbContext db, IHortaAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        public List<Horta> Hortas { get; private set; } = new();
        public bool PodeCriar { get; private set; }
        public string? Mensagem { get; private set; }

        public async Task OnGet()
        {
            var user = await ObterUsuarioAtualAsync();
            if (user is null)
            {
                Mensagem = "Você precisa estar autenticado para gerenciar hortas.";
                Hortas = new();
                PodeCriar = false;
                return;
            }

            PodeCriar = _auth.CanCreate(user);

            if (user.Papel == PapelUsuario.Administrador)
            {
                Hortas = await _db.Hortas
                    .Include(h => h.Usuario)
                    .Include(h => h.HortaProdutos)
                        .ThenInclude(hp => hp.Produto)
                    .OrderByDescending(h => h.CriadoEm)
                    .ToListAsync();
            }
            else if (user.Papel == PapelUsuario.Gerente)
            {
                Hortas = await _db.Hortas
                    .Where(h => h.UsuarioId == user.Id)
                    .Include(h => h.Usuario)
                    .Include(h => h.HortaProdutos)
                        .ThenInclude(hp => hp.Produto)
                    .OrderByDescending(h => h.CriadoEm)
                    .ToListAsync();
            }
            else
            {
                Mensagem = "Sua conta não possui permissão para gerenciar hortas.";
                Hortas = new();
            }
        }

        private async Task<Usuario?> ObterUsuarioAtualAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}