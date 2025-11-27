using FiveMotors.Models;
using FiveMotors.Models.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class WhatsAppController : ControllerBase
{
    private readonly UltraMsgService _service;

    public WhatsAppController()
    {
        _service = new UltraMsgService(
            instanceId: "instance151996",
            token: "ai8muhk6bw35uuzm"
        );
    }

    [HttpPost("Enviar")]
    public async Task<IActionResult> Enviar([FromBody] WhatsAppMessage msg)
    {
        if (msg == null)
            return BadRequest("Corpo vazio.");

        var retorno = await _service.EnviarMensagemAsync(msg.NumeroDestino, msg.Mensagem);

        return Ok(new
        {
            enviado = true,
            resposta = retorno
        });
    }
}
