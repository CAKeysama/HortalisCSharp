using HortalisCSharp.Data;
using HortalisCSharp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Models; // para PapelUsuario
using System.Globalization;
using System.Text;

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

            // Top produtos (contagem por HortaProdutos)
            var topProdutos = await _db.HortaProdutos
                .AsNoTracking()
                .Include(hp => hp.Produto)
                .Where(hp => hp.Produto != null)
                .GroupBy(hp => hp.Produto!.Nome)
                .Select(g => new RelatoriosViewModel.ProductStat { Nome = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(100)
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
                    h.CriadoEm,
                    h.UltimaAlteracao
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
                    UsuarioNome = r.UsuarioNome ?? (r.UsuarioId.HasValue ? "(Usuário excluído)" : "(Sem dono)"),
                    CriadoEm = r.CriadoEm,
                    UltimaAlteracao = r.UltimaAlteracao,
                    ProdutoCount = produtoCount
                });
            }

            // NOVO: lista de usuários para edição rápida de papéis
            var usuarios = await _db.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nome)
                .Select(u => new RelatoriosViewModel.UserItem
                {
                    UsuarioId = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Papel = (int)u.Papel
                })
                .Take(200) // limitar para não carregar tudo de uma vez
                .ToListAsync();

            // NOVO: agregação de indicações por área (centroide + contagem)
            var indicacoesRaw = await _db.Indicacoes
                .AsNoTracking()
                .Where(i => !string.IsNullOrWhiteSpace(i.AreaNome))
                .ToListAsync();

            var indicacoesGrouped = indicacoesRaw
                .GroupBy(i => (i.AreaNome ?? "").Trim().ToLower())
                .Select(g =>
                {
                    var displayName = g.Select(x => x.AreaNome).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? g.Key;
                    var coords = g.Where(x => x.Latitude.HasValue && x.Longitude.HasValue).ToList();
                    double? avgLat = coords.Any() ? coords.Average(x => x.Latitude!.Value) : null;
                    double? avgLng = coords.Any() ? coords.Average(x => x.Longitude!.Value) : null;

                    return new RelatoriosViewModel.IndicacaoArea
                    {
                        AreaNome = displayName!,
                        Latitude = avgLat,    // pode ser null
                        Longitude = avgLng,   // pode ser null
                        Count = g.Count()
                    };
                })
                .OrderByDescending(x => x.Count)
                .Take(200)
                .ToList();

            var vm = new RelatoriosViewModel
            {
                TotalHortas = totalHortas,
                TotalUsuarios = totalUsuarios,
                TotalProdutos = totalProdutos,
                TopProdutos = topProdutos,
                TopUsuarios = topUsuarios,
                MediaProdutosPorHorta = Math.Round(mediaProdutosPorHorta, 2),
                ProdutosSemHorta = produtosSemHorta,
                HortasRecentes = hortaSummaries,
                Usuarios = usuarios,
                IndicacoesPorArea = indicacoesGrouped
            };

            ViewData["Title"] = "Relatórios";
            return View(vm);
        }

        // GET: /Admin/BuscarProdutos?q=...
        [HttpGet]
        public async Task<IActionResult> BuscarProdutos(string q)
        {
            // busca os top produtos (grupo por nome) e filtra localmente usando normalização (remoção de acentos)
            var produtos = await _db.HortaProdutos
                .AsNoTracking()
                .Include(hp => hp.Produto)
                .Where(hp => hp.Produto != null)
                .GroupBy(hp => hp.Produto!.Nome)
                .Select(g => new RelatoriosViewModel.ProductStat { Nome = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(200)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(q))
                return Json(produtos);

            var nq = NormalizeForSearch(q);
            var filtered = produtos.Where(p => NormalizeForSearch(p.Nome).Contains(nq)).ToList();
            return Json(filtered);
        }

        private static string NormalizeForSearch(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        // POST: /Admin/AtualizarPapel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarPapel(int id, int papel)
        {
            if (!Enum.IsDefined(typeof(PapelUsuario), papel))
                return BadRequest("Papel inválido.");

            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.Papel = (PapelUsuario)papel;
            await _db.SaveChangesAsync();

            return Ok(new { id = usuario.Id, papel = (int)usuario.Papel });
        }
    }
}