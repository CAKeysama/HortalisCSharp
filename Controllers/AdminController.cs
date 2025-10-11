using HortalisCSharp.Data;
using HortalisCSharp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HortalisCSharp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Relatorios
        [HttpGet]
        public async Task<IActionResult> Relatorios()
        {
            // Consultas simples e otimizadas
            var totalHortas = await _db.Hortas.AsNoTracking().CountAsync();
            var totalUsuarios = await _db.Usuarios.AsNoTracking().CountAsync();

            // Conta produtos distintos: STRING_SPLIT + alias da coluna como [Value] para o SqlQuery<int>
            var totalProdutos = await _db.Database.SqlQuery<int>(
                $"""
                SELECT COUNT(DISTINCT LTRIM(RTRIM(s.value))) AS [Value]
                FROM [Hortas] WITH (NOLOCK)
                CROSS APPLY STRING_SPLIT(ISNULL([Produtos], ''), ',') AS s
                WHERE LEN(LTRIM(RTRIM(s.value))) > 0
                """
            ).SingleAsync();

            var vm = new RelatoriosViewModel
            {
                TotalHortas = totalHortas,
                TotalUsuarios = totalUsuarios,
                TotalProdutos = totalProdutos
            };

            ViewData["Title"] = "Relatórios";
            return View(vm);
        }
    }
}