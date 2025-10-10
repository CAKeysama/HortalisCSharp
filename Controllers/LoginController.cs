using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using System.Security.Claims;

namespace HortalisCSharp.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Usuario> _hasher;

        public LoginController(AppDbContext db, IPasswordHasher<Usuario> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        // GET: Login
        public IActionResult Index() => View();

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Entrar(string email, string senha)
        {
            var usuario = await _db.Usuarios.SingleOrDefaultAsync(u => u.Email == email);
            if (usuario != null)
            {
                var result = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
                if (result == PasswordVerificationResult.Success)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim(ClaimTypes.Name, usuario.Nome),
                        new Claim(ClaimTypes.Email, usuario.Email),
                        new Claim(ClaimTypes.Role, usuario.Papel.ToString()) // adiciona a role como claim
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = true });

                    TempData["UsuarioLogado"] = usuario.Nome;
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Erro = "Email ou senha inválidos.";
            return View("Index");
        }

        // POST: Cadastro
        [HttpPost]
        public async Task<IActionResult> Cadastrar(string nome, string email, string senha, int papel = 0)
        {
            if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            {
                TempData["Cadastro"] = "Email já cadastrado.";
                return RedirectToAction("Index");
            }

            // normaliza o valor recebido (0=Padrao, 1=Gerente, 2=Administrador)
            if (!Enum.IsDefined(typeof(PapelUsuario), papel))
                papel = (int)PapelUsuario.Padrao;

            var usuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Papel = (PapelUsuario)papel
            };
            usuario.SenhaHash = _hasher.HashPassword(usuario, senha);

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            TempData["Cadastro"] = $"Usuário {nome} cadastrado com sucesso.";
            return RedirectToAction("Index");
        }
    }
}