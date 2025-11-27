using FiveMotors.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace FiveMotors.Controllers
{
    public class ProdutosServicosController : Controller
    {

        // GET: ProdutosServicosController
        public async Task<IActionResult> Index(string modelo, string marca, int? ano)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync("http://localhost:5206/api/Veiculos");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var veiculos = JsonSerializer.Deserialize<List<Veiculo>>(json, options);

            if (veiculos == null)
                veiculos = new List<Veiculo>();

            if (!string.IsNullOrEmpty(modelo)) veiculos = veiculos.Where(v => v.Modelo.Contains(modelo)).ToList();
            if (!string.IsNullOrEmpty(marca)) veiculos = veiculos.Where(v => v.Marca == marca).ToList();
            if (ano != null) veiculos = veiculos.Where(v => v.Ano == ano).ToList();

            ViewBag.Marcas = veiculos.Select(v => v.Marca).Distinct().ToList();
            ViewBag.Anos = veiculos.Select(v => v.Ano).Distinct().ToList();


            return View(veiculos);
        }

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
            {
                return NotFound();
            }



            return View("Especificacao", veiculo);
        }

        /*
        public async Task<IActionResult> Pagamento(Guid id) 
        {
            
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); 
            }

            using var client = new HttpClient();

            
            var jsonVeiculo = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{id}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(jsonVeiculo, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (veiculo == null)
                return NotFound("Veículo não encontrado.");

            
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return BadRequest("Não foi possível identificar o usuário logado.");

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{userId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (cliente == null)
                return BadRequest("Cliente não encontrado.");

            ViewBag.ClienteId = cliente.ClienteId;

            var pagamentoJson = await client.GetStringAsync("http://localhost:5206/api/FormaDePagamamentoes");
            var formasPagamento = JsonSerializer.Deserialize<List<FormaDePagamento>>(pagamentoJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewBag.FormasPagamento = formasPagamento;

           
            return View(veiculo);
        }



        [HttpPost]
        public async Task<IActionResult> ConfirmarPagamento(Guid veiculoId, Guid formaPagamentoId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            
            var jsonVeiculo = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{veiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(jsonVeiculo, options);
            if (veiculo == null)
                return NotFound("Veículo não encontrado.");

            
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Usuário não logado.");

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{userId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, options);
            if (cliente == null)
                return BadRequest("Cliente não encontrado.");

           
            var jsonForma = await client.GetStringAsync($"http://localhost:5206/api/FormaDePagamamentoes/{formaPagamentoId}");
            var forma = JsonSerializer.Deserialize<FormaDePagamento>(jsonForma, options);
            if (forma == null)
                return BadRequest("Forma de pagamento não encontrada.");

            
            var vendaParaCriar = new Venda
            {
                ClienteId = cliente.ClienteId,
                VeiculoId = veiculo.VeiculosId,
                FormaDePagamentoId = forma.FormaDePagamentoId,
                Status = "Concluída",
                DataPrevistaEntrega = DateTime.Now.AddDays(7),
                Preco = veiculo.Preco,
                Descricao = $"Venda do veículo {veiculo.Marca} {veiculo.Modelo}",
                DataHora = DateTime.Now
            };

            var contentVenda = new StringContent(JsonSerializer.Serialize(vendaParaCriar), Encoding.UTF8, "application/json");
            var responseVenda = await client.PostAsync("http://localhost:5206/api/Vendas", contentVenda);
            if (!responseVenda.IsSuccessStatusCode)
            {
                var erro = await responseVenda.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao criar a venda: {erro}");
            }

           
            var jsonVendaCriada = await responseVenda.Content.ReadAsStringAsync();
            var vendaCriada = JsonSerializer.Deserialize<Venda>(jsonVendaCriada, options);
            if (vendaCriada == null || vendaCriada.VendaId == Guid.Empty)
                return BadRequest("Erro ao obter a venda criada.");

           
            var item = new ItemDaVenda
            {
                ItemDaVendaId = Guid.NewGuid(),
                VendaId = vendaCriada.VendaId,
                VeiculoId = veiculo.VeiculosId,
                Quantidade = 1,
                PrecoUnitario = veiculo.Preco,
                Total = veiculo.Preco
            };

            var contentItem = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
            var responseItem = await client.PostAsync("http://localhost:5206/api/ItemDaVendas", contentItem);
            if (!responseItem.IsSuccessStatusCode)
            {
                var erroItem = await responseItem.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao criar item da venda: {erroItem}");
            }

            var estoqueJson = await client.GetStringAsync($"http://localhost:5206/api/EstoquePedido/veiculo/{veiculo.VeiculosId}");
            var estoque = JsonSerializer.Deserialize<EstoquePedido>(estoqueJson, options);
            if (estoque != null && estoque.QuantidadeDisponivel > 0)
            {
                var novaQuantidade = estoque.QuantidadeDisponivel - 1;
                await client.PutAsync($"http://localhost:5206/api/EstoquePedido/{estoque.EstoquePedidoId}?novaQuantidade={novaQuantidade}", null);
            }

           
            return RedirectToAction("PagamentoConfirmado", new { vendaId = vendaCriada.VendaId });
        }



        public async Task<IActionResult> PagamentoConfirmado(Guid vendaId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

          
            var jsonVenda = await client.GetStringAsync($"http://localhost:5206/api/Vendas/{vendaId}");
            var venda = JsonSerializer.Deserialize<Venda>(jsonVenda, options);
            if (venda == null)
                return NotFound("Venda não encontrada.");

          
            var jsonVeiculo = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{venda.VeiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(jsonVeiculo, options);
            if (veiculo == null)
                return NotFound("Veículo não encontrado.");

            
            var jsonForma = await client.GetStringAsync($"http://localhost:5206/api/FormaDePagamamentoes/{venda.FormaDePagamentoId}");
            var forma = JsonSerializer.Deserialize<FormaDePagamento>(jsonForma, options);
            if (forma == null)
                return NotFound("Forma de pagamento não encontrada.");

           
            ViewBag.Veiculo = veiculo;
            ViewBag.FormaPagamento = forma;

            return View(venda);
        }

        */

        
        [HttpPost]
        [Authorize()]
        public async Task<IActionResult> Interesse(Guid veiculoId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1. Buscar veículo
            var jsonVeiculo = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{veiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(jsonVeiculo, options);

            if (veiculo == null)
                return NotFound("Veículo não encontrado.");

            // 2. Buscar o cliente logado
            var clienteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (clienteId == null)
                return Unauthorized("Usuário não logado.");

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{clienteId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, options);

            if (cliente == null)
                return BadRequest("Não foi possível carregar os dados do cliente.");

            // 3. Montar mensagem
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

<b>Data da solicitação:</b> {DateTime.Now:dd/MM/yyyy HH:mm}<br><br>

As imagens seguem anexadas a este e-mail.
";

            // 4. BAIXAR AS IMAGENS DO VEÍCULO E CONVERTER PARA BASE64
            var anexos = new List<string>();

            if (veiculo.ImagemsVeiculo != null && veiculo.ImagemsVeiculo.Any())
            {
                foreach (var imgUrl in veiculo.ImagemsVeiculo)
                {
                    try
                    {
                        // garante que é URL pública absoluta
                        if (!imgUrl.StartsWith("http"))
                            continue; // ignora caso não seja URL completa

                        var bytes = await client.GetByteArrayAsync(imgUrl);
                        var base64 = Convert.ToBase64String(bytes);

                        anexos.Add(base64);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao baixar imagem: {imgUrl} | {ex.Message}");
                    }
                }
            }

            // 5. Montar payload para API de e-mail
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

            TempData["Mensagem"] = "Seu interesse foi enviado! Em breve entraremos em contato.";

            return RedirectToAction("Especificacao", new { id = veiculoId });
        }



        [Authorize()]
        public async Task<IActionResult> EnviarWhatsapp(Guid veiculoId)
        {
            using var client = new HttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


            // 1 — Buscar o veículo
            var json = await client.GetStringAsync($"http://localhost:5206/api/Veiculos/{veiculoId}");
            var veiculo = JsonSerializer.Deserialize<Veiculo>(json, options);

            if (veiculo == null)
            {
                TempData["msg"] = "Veículo não encontrado.";
                return RedirectToAction("Especificacao", new { id = veiculoId });
            }

            var clienteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (clienteId == null)
                return Unauthorized("Usuário não logado.");

            var jsonCliente = await client.GetStringAsync($"http://localhost:5206/api/Clientes/usuario/{clienteId}");
            var cliente = JsonSerializer.Deserialize<Cliente>(jsonCliente, options);

            // 2 — Número de destino (PRECISA estar autorizado na API do WhatsApp)
            string numeroDestino = "5514998229788"; // <-- coloque um número válido

            // 3 — Montar mensagem
            string mensagem =
             $"Olá sou {cliente.Nome}, meu email {cliente.Email} e meu CPF é {cliente.CpfCnpj},! Estou entrando em contato para solicitar informações detalhadas sobre o veículo abaixo:\n\n" +

             $"📌 *Veículo:* {veiculo.Marca} {veiculo.Modelo}\n" +
             $"📆 *Ano:* {veiculo.Ano}\n" +
            $"💰 *Preço:* {veiculo.Preco:C}\n\n" +
            $"📝 *Descrição:* {veiculo.Descricao}\n\n" +
            $"📸 *Imagens do veículo:* \n" +
            string.Join("\n", veiculo.ImagemsVeiculo.Select(img => $"• {img}")) +
            "\n\n" +

            "Para prosseguir, poderia me informar, por gentileza:\n\n" +
            "1️⃣ O valor final do veículo e possíveis condições de negociação?\n" +
            "2️⃣ Opções de pagamento e financiamento disponíveis?\n" +
            "3️⃣ Disponibilidade atual do modelo para visita ou test-drive?\n" +
            "4️⃣ Se há garantia e qual sua duração?\n\n" +
            "Aguardo o retorno. Obrigado!" +
            $"Entre em contato{cliente.Telefone}";

            // 4 — Payload para API
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

            // 5 — Chamada para API WhatsApp
            var response = await client.PostAsync(
                "http://localhost:5206/api/WhatsApp/Enviar",
                content
            );

            var result = await response.Content.ReadAsStringAsync();

            // 6 — Exibir retorno no front
            TempData["msg"] = result;

            return RedirectToAction("Especificacao", new { id = veiculoId });
        }

         


        // GET: ProdutosServicosController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProdutosServicosController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProdutosServicosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProdutosServicosController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProdutosServicosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProdutosServicosController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ProdutosServicosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
