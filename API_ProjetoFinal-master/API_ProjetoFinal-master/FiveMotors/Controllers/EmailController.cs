using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using FiveMotors.Models;
using static FiveMotors.Models.EmailServices;

namespace FiveMotors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            var emailSender = new SimpleEmailSender(_configuration);

            await emailSender.SendEmailAsync(
                to: request.To,
                subject: request.Subject,
                body: request.Message,
                isHtml: true,
                attachmentsBase64: request.AttachmentsBase64 
            );

            return Ok("E-mail enviado com sucesso!");
        }
    }
}
