using FiveMotors.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace FiveMotors.Controllers
{
    public class ProdutosServicosController : Controller
    {
        // LISTAGEM + FILTRO DE VEÍCULOS
        public async Task<IActionResult> Index(string modelo, string marca, int? ano)
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

            // Filtros
            if (!string.IsNullOrEmpty(modelo))
                veiculos = veiculos.Where(v => v.Modelo.Contains(modelo)).ToList();

            if (!string.IsNullOrEmpty(marca))
                veiculos = veiculos.Where(v => v.Marca == marca).ToList();

            if (ano != null)
                veiculos = veiculos.Where(v => v.Ano == ano).ToList();

            // Listas para dropdowns
            ViewBag.Marcas = veiculos.Select(v => v.Marca).Distinct().ToList();
            ViewBag.Anos = veiculos.Select(v => v.Ano).Distinct().ToList();

            return View(veiculos);
        }

        // EXIBIR DETALHES DO VEÍCULO
        public async Task<IActionResult> Especificacao(Guid id)
        {
            using var client = new HttpClient();

            var json = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{id}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var veiculo = JsonSerializer.Deserialize<Veiculo>(json, options);

            if (veiculo == null)
                return NotFound();

            return View("Especificacao", veiculo);
        }

        /*  ------------------------------
            MÉTODOS DE PAGAMENTO (COMPLETOS)
            COMENTADOS — NÃO ALTERADOS
        ------------------------------ */

        // REGISTRAR INTERESSE POR E-MAIL
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Interesse(Guid veiculoId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Buscar veículo
            var jsonVeiculo = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{veiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(jsonVeiculo, options);

            if (veiculo == null)
                return NotFound("Veículo não encontrado.");

            // Buscar cliente logado
            var clienteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (clienteId == null)
                return Unauthorized("Usuário não logado.");

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{clienteId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, options);

            if (cliente == null)
                return BadRequest("Cliente não encontrado.");

            // Montar mensagem HTML
            string mensagem = $@"
📣 <b>Registro de Interesse – FiveMotors</b><br><br>
<b>Veículo:</b><br>
• Modelo: {veiculo.Marca} {veiculo.Modelo}<br>
• Ano: {veiculo.Ano}<br>
• Preço: {veiculo.Preco:C}<br>
• Descrição: {veiculo.Descricao}<br><br>
<b>Cliente:</b><br>
• Nome: {cliente.Nome}<br>
• E-mail: {cliente.Email}<br>
• CPF: {cliente.CpfCnpj}<br><br>
<b>Data:</b> {DateTime.Now:dd/MM/yyyy HH:mm}<br><br>
As imagens seguem anexadas.
";

            // Baixar imagens e converter para Base64
            var anexos = new List<string>();

            if (veiculo.ImagemsVeiculo != null && veiculo.ImagemsVeiculo.Any())
            {
                foreach (var imgUrl in veiculo.ImagemsVeiculo)
                {
                    try
                    {
                        if (!imgUrl.StartsWith("http"))
                            continue;

                        var bytes = await client.GetByteArrayAsync(imgUrl);
                        anexos.Add(Convert.ToBase64String(bytes));
                    }
                    catch { }
                }
            }

            // Payload para API de e-mail
            var dados = new
            {
                to = "ponteseduardo011@gmail.com",
                subject = $"Interesse no veículo {veiculo.Marca} {veiculo.Modelo}",
                message = mensagem,
                attachmentsBase64 = anexos
            };

            var content = new StringContent(
                JsonSerializer.Serialize(dados),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("http://localhost:5206/api/Email/send", content);

            if (!response.IsSuccessStatusCode)
                return BadRequest("Erro ao enviar interesse por e-mail.");

            TempData["Mensagem"] = "Seu interesse foi enviado com sucesso!";

            return RedirectToAction("Especificacao", new { id = veiculoId });
        }

        // ENVIAR MENSAGEM NO WHATSAPP
        [Authorize]
        public async Task<IActionResult> EnviarWhatsapp(Guid veiculoId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Buscar veículo
            var json = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{veiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(json, options);

            if (veiculo == null)
            {
                TempData["msg"] = "Veículo não encontrado.";
                return RedirectToAction("Especificacao", new { id = veiculoId });
            }

            // Buscar cliente
            var clienteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (clienteId == null)
                return Unauthorized();

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{clienteId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, options);

            // Número autorizado no WhatsApp
            string numeroDestino = "5514998229788";

            // Montar mensagem
            string mensagem =
                $"Olá, sou {cliente.Nome}! Meu e-mail é {cliente.Email} e CPF {cliente.CpfCnpj}.\n\n" +
                $"Tenho interesse no veículo:\n" +
                $"📌 {veiculo.Marca} {veiculo.Modelo}\n" +
                $"📆 Ano: {veiculo.Ano}\n" +
                $"💰 Preço: {veiculo.Preco:C}\n\n" +
                $"📝 Descrição: {veiculo.Descricao}\n\n" +
                $"📸 Imagens:\n" +
                string.Join("\n", veiculo.ImagemsVeiculo.Select(img => $"• {img}\n\n\n")) +
                $"Retorne a conversa com o número : {cliente.Telefone}";

            // Payload
            var payload = new
            {
                NumeroDestino = numeroDestino,
                Mensagem = mensagem
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            // Envio
            var response = await client.PostAsync("http://localhost:5206/api/WhatsApp/Enviar", content);

            TempData["msg"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction("Especificacao", new { id = veiculoId });
        }

        // CRUD GERADO AUTOMATICAMENTE — NÃO ALTERADO
        public ActionResult Details(int id) => View();
        public ActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try { return RedirectToAction(nameof(Index)); }
            catch { return View(); }
        }

        public ActionResult Edit(int id) => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try { return RedirectToAction(nameof(Index)); }
            catch { return View(); }
        }

        public ActionResult Delete(int id) => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try { return RedirectToAction(nameof(Index)); }
            catch { return View(); }
        }
    }
}
