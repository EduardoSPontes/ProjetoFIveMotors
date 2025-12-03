using FiveMotors.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace FiveMotors.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Página inicial
        public async Task<IActionResult> Index()
        {
            using var client = new HttpClient();

            // Busca todos os veículos da API
            var json = await client.GetStringAsync("http://localhost:5206/api/Veiculos");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var veiculos = JsonSerializer.Deserialize<List<Veiculo>>(json, options)
                           ?? new List<Veiculo>();

            // Seleciona apenas 3 veículos para destaque
            var destaques = veiculos.Take(3).ToList();

            return View(destaques);
        }

        public IActionResult QuemSomos()
        {
            return View();
        }

        public IActionResult ProdutosServicos()
        {
            return View();
        }

        // Página de erro padrão
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
