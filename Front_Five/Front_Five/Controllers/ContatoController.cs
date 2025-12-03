using FiveMotors.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace FiveMotors.Controllers
{
    public class ContatoController : Controller
    {
        // Página inicial do formulário de contato
        public IActionResult Index()
        {
            return View();
        }

        // Recebe os dados do formulário e envia para a API
        [HttpPost]
        public async Task<IActionResult> EnviarMensagem(FaleComVendedor model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            using var client = new HttpClient();

            // Envia a mensagem para a API
            var response = await client.PostAsJsonAsync(
                "http://localhost:5206/api/FaleComVendedor", model
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Sucesso"] = "Mensagem enviada com sucesso!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Erro"] = "Falha ao enviar mensagem. Tente novamente.";
                return View("Index", model);
            }
        }
    }
}
