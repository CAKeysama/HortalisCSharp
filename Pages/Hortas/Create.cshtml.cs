using System.Security.Claims;
using HortalisCSharp.Data;
using HortalisCSharp.Models;
using HortalisCSharp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;

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

        // campo usado para UI (lista de nomes separados por vírgula)
        [BindProperty]
        public string? ProdutoNomes { get; set; }

        // Raw coordenadas recebidas do formulário (strings) — permitem parsing controlado
        [BindProperty]
        public string? LatitudeRaw { get; set; }

        [BindProperty]
        public string? LongitudeRaw { get; set; }

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

            // Parse robusto das coordenadas antes da validação do modelo
            var latParsed = TryParseCoordinate(LatitudeRaw, isLatitude: true, out var latVal);
            var lngParsed = TryParseCoordinate(LongitudeRaw, isLatitude: false, out var lngVal);

            if (!latParsed) ModelState.AddModelError(nameof(LatitudeRaw), "Formato de latitude inválido. Use -21.634599 ou -21 38 0.0 (DMS) ou com vírgula.");
            if (!lngParsed) ModelState.AddModelError(nameof(LongitudeRaw), "Formato de longitude inválido. Use -48.343873 ou -48 20 37.0 (DMS) ou com vírgula.");

            if (!latParsed || !lngParsed)
            {
                // mantém os erros de parsing visíveis e retorna para a página
                return Page();
            }

            // atribui valores parseados ao modelo
            Input.Latitude = latVal;
            Input.Longitude = lngVal;

            // remove eventuais erros automáticos prévios para Input.Latitude/Input.Longitude
            ModelState.Remove("Input.Latitude");
            ModelState.Remove("Input.Longitude");

            // valida o modelo (agora com valores numéricos atribuídos)
            if (!TryValidateModel(Input, nameof(Input)))
                return Page();

            Input.CriadoEm = DateTime.UtcNow;
            Input.UltimaAlteracao = Input.CriadoEm;
            Input.UsuarioId = user.Id;

            _db.Hortas.Add(Input);
            await _db.SaveChangesAsync();

            var nomes = (ProdutoNomes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var nome in nomes)
            {
                var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Nome.ToLower() == nome.ToLower());
                if (produto is null)
                {
                    produto = new Produto { Nome = nome };
                    _db.Produtos.Add(produto);
                    await _db.SaveChangesAsync();
                }

                var hpExists = await _db.HortaProdutos.AnyAsync(hp => hp.HortaId == Input.Id && hp.ProdutoId == produto.Id);
                if (!hpExists)
                {
                    _db.HortaProdutos.Add(new HortaProduto { HortaId = Input.Id, ProdutoId = produto.Id });
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private async Task<Usuario?> ObterUsuarioAtualAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Tenta parsear coordenada aceitando:
        /// - decimal com ponto ou vírgula (ex: -21.634599 ou -21,634599)
        /// - notação científica (ex: -2.1634599E+01)
        /// - DMS (graus minutos segundos): "23 34 12.5 S" ou "23°34'12.5\" S"
        /// Também corrige valores escalados (ex.: -21634599 -> -21.634599) detectando magnitude anômala.
        /// </summary>
        private static bool TryParseCoordinate(string? raw, bool isLatitude, out double value)
        {
            value = double.NaN;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var s = raw.Trim();

            // Remove caracteres de espaço duplicados
            s = Regex.Replace(s, @"\s+", " ");

            // Detectar DMS (graus minutos segundos) - procurar sequências de números
            var dmsMatches = Regex.Matches(s, @"-?\d+(\.\d+)?");
            if (dmsMatches.Count >= 2 && (s.Contains("°") || s.Contains("'") || s.Contains("\"") || dmsMatches.Count >= 3))
            {
                // extrai números (graus, minutos, segundos) da string
                var nums = dmsMatches.Select(m => double.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture)).ToList();
                double deg = nums.ElementAtOrDefault(0);
                double min = nums.ElementAtOrDefault(1);
                double sec = nums.ElementAtOrDefault(2);

                double sign = 1.0;
                if (s.IndexOf('S', StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf('W', StringComparison.OrdinalIgnoreCase) >= 0) sign = -1.0;
                if (deg < 0) sign = deg < 0 ? -1.0 : sign; // mantém sinal do grau quando presente

                var decimalDeg = Math.Abs(deg) + (Math.Abs(min) / 60.0) + (Math.Abs(sec) / 3600.0);
                value = sign * decimalDeg;
            }
            else
            {
                // Normaliza vírgula para ponto e remove caracteres extras (grau símbolo, etc.)
                var cleaned = s.Replace(',', '.');
                cleaned = cleaned.Replace("°", " ").Replace("º", " ").Replace("º", " ").Replace("′", " ").Replace("’", " ").Replace("’", " ").Replace("''", " ").Trim();

                // Manter apenas caracteres relevantes para float (digits, + - . e exponent)
                var m = Regex.Match(cleaned, @"[+-]?\d+(\.\d+)?([eE][+-]?\d+)?");
                if (!m.Success) return false;

                var numStr = m.Value;
                if (!double.TryParse(numStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                {
                    // tenta com CurrentCulture como fallback
                    if (!double.TryParse(numStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsed))
                        return false;
                }

                value = parsed;
            }

            // Auto-correção: se valor muito grande (escalado por 1e6), divide
            if (double.IsFinite(value) && Math.Abs(value) > 1000)
            {
                value = value / 1_000_000.0;
            }

            // valida intervalo básico
            if (isLatitude)
            {
                if (double.IsNaN(value) || value < -90.0 || value > 90.0) return false;
            }
            else
            {
                if (double.IsNaN(value) || value < -180.0 || value > 180.0) return false;
            }

            return true;
        }
    }
}