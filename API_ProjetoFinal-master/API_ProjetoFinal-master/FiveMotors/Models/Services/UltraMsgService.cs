using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace FiveMotors.Models.Services
{
    public class UltraMsgService
    {
        private readonly string _instanceId;
        private readonly string _token;
        private readonly HttpClient _http;

        public UltraMsgService(string instanceId, string token)
        {
            _instanceId = instanceId;
            _token = token;
            _http = new HttpClient();
        }

        public async Task<string> EnviarMensagemAsync(string numeroDestino, string texto)
        {
            var url = $"https://api.ultramsg.com/{_instanceId}/messages/chat?token={_token}";

            var data = new
            {
                to = numeroDestino,   
                body = texto         
            };

            var json = JsonConvert.SerializeObject(data);

            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _http.PostAsync(url, content);

            return await response.Content.ReadAsStringAsync();
        }
    }
}

