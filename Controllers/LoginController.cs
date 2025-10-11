using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Globalization;

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
        [AllowAnonymous]
        public IActionResult Index() => View();

        // POST: Login
        [HttpPost]
        [AllowAnonymous]
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
                        new Claim(ClaimTypes.Role, usuario.Papel.ToString())
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = true });

                    HttpContext.Session.SetInt32("UserId", usuario.Id);
                    HttpContext.Session.SetString("UserName", usuario.Nome);
                    HttpContext.Session.SetString("UserEmail", usuario.Email);
                    HttpContext.Session.SetString("UserCreatedAt", usuario.CriadoEm.ToString("o", CultureInfo.InvariantCulture));

                    TempData["UsuarioLogado"] = usuario.Nome;
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Erro = "Email ou senha inválidos.";
            return View("Index");
        }

        // POST: Cadastro
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Cadastrar(string nome, string email, string senha, int papel = 0)
        {
            if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            {
                TempData["Cadastro"] = "Email já cadastrado.";
                return RedirectToAction("Index");
            }

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

        // GET: /Login/Conta
        [HttpGet]
        public IActionResult Conta()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null)
                return Redirect("/Login/Index");

            var name = HttpContext.Session.GetString("UserName") ?? string.Empty;
            var email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var createdAtIso = HttpContext.Session.GetString("UserCreatedAt");

            DateTime? createdAt = null;
            if (!string.IsNullOrWhiteSpace(createdAtIso) &&
                DateTime.TryParse(createdAtIso, null, DateTimeStyles.RoundtripKind, out var dt))
            {
                createdAt = dt;
            }

            ViewData["Title"] = "Minha Conta";
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.CreatedAt = createdAt;

            return View();
        }

        // GET: /Login/TrocarSenha
        [HttpGet]
        public IActionResult TrocarSenha()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null)
                return Redirect("/Login/Index");

            ViewData["Title"] = "Trocar senha";
            return View();
        }

        // POST: /Login/TrocarSenha
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrocarSenha(string senhaAtual, string novaSenha, string confirmarSenha)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null)
                return Redirect("/Login/Index");

            if (string.IsNullOrWhiteSpace(senhaAtual) ||
                string.IsNullOrWhiteSpace(novaSenha) ||
                string.IsNullOrWhiteSpace(confirmarSenha))
            {
                ViewBag.Erro = "Preencha todos os campos.";
                return View();
            }

            if (novaSenha.Length < 8)
            {
                ViewBag.Erro = "A nova senha deve ter pelo menos 8 caracteres.";
                return View();
            }

            if (!string.Equals(novaSenha, confirmarSenha, StringComparison.Ordinal))
            {
                ViewBag.Erro = "A confirmação de senha não confere.";
                return View();
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (usuario is null)
            {
                ViewBag.Erro = "Usuário não encontrado.";
                return View();
            }

            var verify = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senhaAtual);
            if (verify != PasswordVerificationResult.Success)
            {
                ViewBag.Erro = "Sua senha atual está incorreta.";
                return View();
            }

            usuario.SenhaHash = _hasher.HashPassword(usuario, novaSenha);
            await _db.SaveChangesAsync();

            TempData["ContaMsg"] = "Senha alterada com sucesso.";
            return RedirectToAction(nameof(Conta));
        }

        // GET: /Login/Sair
        [HttpGet]
        public async Task<IActionResult> Sair()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}