using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SeuProjeto.Controllers
{
    public class LoginController : Controller
    {
        // Mock de usuários
        private List<Usuario> usuariosMock = new List<Usuario>
        {
            new Usuario { Nome = "João Silva", Email = "joao@teste.com", Senha = "123456" },
            new Usuario { Nome = "Maria Souza", Email = "maria@teste.com", Senha = "654321" }
        };

        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Entrar(string email, string senha)
        {
            var usuario = usuariosMock.FirstOrDefault(u => u.Email == email && u.Senha == senha);
            if (usuario != null)
            {
                TempData["UsuarioLogado"] = usuario.Nome;
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Erro = "Email ou senha inválidos.";
            return View("Index");
        }

        // POST: Cadastro
        [HttpPost]
        public ActionResult Cadastrar(string nome, string email, string senha)
        {
            // Apenas simula cadastro sem salvar
            TempData["Cadastro"] = $"Usuário {nome} cadastrado (simulação).";
            return RedirectToAction("Index");
        }
    }

    // Adicione a definição da classe Usuario se ela não existir em outro arquivo
    public class Usuario
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
    }
}