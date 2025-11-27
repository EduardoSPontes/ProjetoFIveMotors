using FiveMotors.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiveMotors.Controllers
{
    public class ContatoController : Controller
    {
        // GET: ContatoController
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensagem(FaleComVendedor model)
        {
          
            using var client = new HttpClient();

            var response = await client.PostAsJsonAsync("http://localhost:5206/api/FaleComVendedor", model);

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


        // GET: ContatoController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContatoController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContatoController/Create
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

        // GET: ContatoController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContatoController/Edit/5
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

        // GET: ContatoController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ContatoController/Delete/5
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
