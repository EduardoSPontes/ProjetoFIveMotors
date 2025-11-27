using System.Net.Mail;
using System.Net;

namespace FiveMotors.Models
{
    public class EmailServices
    {
        public class SimpleEmailSender
        {
            private readonly string _smtpServer;
            private readonly int _port;
            private readonly string _senderEmail;
            private readonly string _senderName;
            private readonly string _username;
            private readonly string _password;

            public SimpleEmailSender(IConfiguration configuration)
            {
                var emailConfig = configuration.GetSection("EmailSettings");
                _smtpServer = emailConfig.GetValue<string>("SmtpServer");
                _port = emailConfig.GetValue<int>("Port");
                _senderEmail = emailConfig.GetValue<string>("SenderEmail");
                _senderName = emailConfig.GetValue<string>("SenderName");
                _username = emailConfig.GetValue<string>("Username");
                _password = emailConfig.GetValue<string>("Password");
            }

            public async Task SendEmailAsync(
                string to,
                string subject,
                string body,
                bool isHtml = true,
                List<string>? attachmentsBase64 = null)
            {
                using var client = new SmtpClient(_smtpServer, _port)
                {
                    Credentials = new NetworkCredential(_username, _password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mail.To.Add(to);

                // 📎 Anexar imagens (base64 → bytes → anexo)
                if (attachmentsBase64 != null)
                {
                    int count = 1;

                    foreach (var base64 in attachmentsBase64)
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(base64);
                            var stream = new MemoryStream(bytes);

                            var attachment = new Attachment(stream, $"imagem_{count}.jpg", "image/jpeg");
                            mail.Attachments.Add(attachment);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Falha ao converter imagem base64: " + ex.Message);
                        }
                    }
                }

                await client.SendMailAsync(mail);
            }
        }
    }
}
