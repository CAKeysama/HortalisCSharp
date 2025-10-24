using System.Diagnostics;
using HortalisCSharp.Models;
using HortalisCSharp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text;

namespace HortalisCSharp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Tutoriais()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Autores()
        {
            return View();
        }

        // POST: /Home/EnviarIndicacao
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarIndicacao(IndicacaoInputModel model)
        {
            if (model == null)
                return BadRequest();

            // Parse robusto de latitude/longitude (aceita "." e ","; tenta invariant e current)
            double? lat = null;
            double? lng = null;

            double? TryParseCoord(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var s = raw.Trim();

                // Normaliza vírgula -> ponto
                s = s.Replace(',', '.');

                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v))
                    return v;
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out v))
                    return v;
                return null;
            }

            lat = TryParseCoord(model.Latitude);
            lng = TryParseCoord(model.Longitude);

            // Auto-correção: se valores estiverem escalados (ex.: -21634599), divide por 1e6
            if (lat.HasValue && Math.Abs(lat.Value) > 1000) lat = lat.Value / 1_000_000.0;
            if (lng.HasValue && Math.Abs(lng.Value) > 1000) lng = lng.Value / 1_000_000.0;

            // Exige coords válidas para persistir
            if (!lat.HasValue || !lng.HasValue)
            {
                ModelState.AddModelError("Coords", "Latitude/Longitude devem ser definidas (use o mapa).");
            }

            if (!ModelState.IsValid)
            {
                TempData["IndicacaoErro"] = "Dados inválidos ao enviar indicação.";
                return RedirectToAction("Index");
            }

            // Tenta obter nome do bairro via reverse geocoding (OpenStreetMap/Nominatim)
            string? areaName = null;
            try
            {
                areaName = await ReverseGeocodeNeighborhoodAsync(lat.Value, lng.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reverse geocode falhou, usando fallback.");
            }

            // Se reverse geocode não encontrou nome, usa grade como fallback determinístico
            if (string.IsNullOrWhiteSpace(areaName))
            {
                areaName = ComputeAreaName(lat.Value, lng.Value);
            }

            var ent = new Indicacao
            {
                Nome = model.Nome?.Trim(),
                Email = model.Email?.Trim(),
                AreaNome = areaName,
                Latitude = lat,
                Longitude = lng,
                CriadoEm = DateTime.UtcNow
            };

            _db.Indicacoes.Add(ent);
            await _db.SaveChangesAsync();

            TempData["IndicacaoSucesso"] = "Indicação enviada com sucesso. Obrigado!";
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Input model reduzido: apenas campos essenciais (lat/lng como string para parsing manual)
        public class IndicacaoInputModel
        {
            public string? Nome { get; set; }
            public string? Email { get; set; }

            // Recebido do form como texto para permitir parsing controlado
            public string? Latitude { get; set; }
            public string? Longitude { get; set; }
        }

        // --- Helpers ---

        // Determina um nome de área por grade (falso-bairro) caso o geocoder não retorne nome válido.
        private static string ComputeAreaName(double latitude, double longitude)
        {
            // Agrupamento por grade: arredonda para 3 casas (~100m de precisão)
            var latR = Math.Round(latitude, 3);
            var lngR = Math.Round(longitude, 3);
            return $"area_{latR:F3}_{lngR:F3}";
        }

        // Chama Nominatim (OpenStreetMap) para obter campos de endereço e extrair bairro/suburb/neighbourhood.
        // Observações:
        // - Nominatim requer User-Agent apropriado e limite de uso. Em produção considere cache e/ou serviço pago.
        private static async Task<string?> ReverseGeocodeNeighborhoodAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            using var client = new HttpClient();
            // User-Agent obrigatório para Nominatim; substitua contato conforme necessidade.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HortalisCSharp/1.0 (+https://yourdomain.example)");
            var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lng.ToString(CultureInfo.InvariantCulture)}&addressdetails=1";

            using var resp = await client.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode) return null;

            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("address", out var addr)) return null;

            // Prioridade comum para "bairro"-like fields
            string[] keys = { "neighbourhood", "suburb", "city_district", "quarter", "town", "village", "hamlet", "county", "city" };

            foreach (var k in keys)
            {
                if (addr.TryGetProperty(k, out var val) && val.ValueKind == JsonValueKind.String)
                {
                    var raw = val.GetString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var cleaned = SanitizeAreaName(raw);
                        // Garantir tamanho e retorno
                        if (!string.IsNullOrWhiteSpace(cleaned))
                            return cleaned.Length <= 120 ? cleaned : cleaned.Substring(0, 120);
                    }
                }
            }

            return null;
        }

        // Remove caracteres problemáticos, normaliza espaços e acentos (opcional)
        private static string SanitizeAreaName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;

            // Trim and collapse spaces
            var t = Regex.Replace(s.Trim(), @"\s+", " ");

            // Opcional: remover acentos para padronizar (se preferir manter acentos, remova as próximas linhas)
            var normalized = t.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var noAccents = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

            // Replace any non-alphanumeric basic characters with underscore (preserva espaços)
            noAccents = Regex.Replace(noAccents, @"[^\w\s\-]", string.Empty);

            return noAccents;
        }
    }
}
