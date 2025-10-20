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
            var totalHortas = await _db.Hortas.AsNoTracking().CountAsync();
            var totalUsuarios = await _db.Usuarios.AsNoTracking().CountAsync();

            // Total de produtos distintos (somente tabela Produtos normalizada)
            var totalProdutos = await _db.Produtos.AsNoTracking().CountAsync();

            // Top produtos (contagem por HortaProdutos) — pode retornar mais itens para permitir busca/rolagem
            var topProdutos = await _db.HortaProdutos
                .AsNoTracking()
                .Include(hp => hp.Produto)
                .Where(hp => hp.Produto != null)
                .GroupBy(hp => hp.Produto!.Nome)
                .Select(g => new RelatoriosViewModel.ProductStat { Nome = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(100) // aumentar limite para permitir melhor visualização; criar página dedicada se necessário
                .ToListAsync();

            // Top usuários por número de hortas
            var userCounts = await _db.Hortas
                .AsNoTracking()
                .GroupBy(h => h.UsuarioId)
                .Select(g => new { UsuarioId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var topUsuarios = new List<RelatoriosViewModel.UserStat>();
            foreach (var uc in userCounts)
            {
                var nome = uc.UsuarioId.HasValue
                    ? await _db.Usuarios.AsNoTracking().Where(u => u.Id == uc.UsuarioId.Value).Select(u => u.Nome).FirstOrDefaultAsync()
                    : "(Não informado)";
                topUsuarios.Add(new RelatoriosViewModel.UserStat
                {
                    UsuarioId = uc.UsuarioId,
                    Nome = nome ?? "(Sem nome)",
                    Count = uc.Count
                });
            }

            // Média de produtos por horta (usa HortaProdutos)
            var totalAssociacoes = await _db.HortaProdutos.AsNoTracking().CountAsync();
            double mediaProdutosPorHorta = totalHortas == 0 ? 0 : totalAssociacoes / (double)totalHortas;

            // Produtos sem horta (produtos cadastrados sem associações)
            var produtosSemHorta = await _db.Produtos.AsNoTracking().Where(p => !p.HortaProdutos.Any()).CountAsync();

            // Últimas 5 hortas com contagem de produtos (baseada em HortaProdutos)
            var recentes = await _db.Hortas
                .AsNoTracking()
                .Include(h => h.Usuario)
                .OrderByDescending(h => h.CriadoEm)
                .Take(5)
                .Select(h => new
                {
                    h.Id,
                    h.Nome,
                    UsuarioId = h.UsuarioId,
                    UsuarioNome = h.Usuario != null ? h.Usuario.Nome : null,
                    h.CriadoEm
                })
                .ToListAsync();

            var hortaSummaries = new List<RelatoriosViewModel.HortaSummary>();
            foreach (var r in recentes)
            {
                var produtoCount = await _db.HortaProdutos.AsNoTracking().Where(hp => hp.HortaId == r.Id).CountAsync();

                hortaSummaries.Add(new RelatoriosViewModel.HortaSummary
                {
                    Id = r.Id,
                    Nome = r.Nome,
                    // define texto coerente para visualização:
                    UsuarioNome = r.UsuarioNome ?? (r.UsuarioId.HasValue ? "(Usuário excluído)" : "(Sem dono)"),
                    CriadoEm = r.CriadoEm,
                    ProdutoCount = produtoCount
                });
            }

            var vm = new RelatoriosViewModel
            {
                TotalHortas = totalHortas,
                TotalUsuarios = totalUsuarios,
                TotalProdutos = totalProdutos,
                TopProdutos = topProdutos,
                TopUsuarios = topUsuarios,
                MediaProdutosPorHorta = Math.Round(mediaProdutosPorHorta, 2),
                ProdutosSemHorta = produtosSemHorta,
                HortasRecentes = hortaSummaries
            };

            ViewData["Title"] = "Relatórios";
            return View(vm);
        }
    }
}