using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GEPS.Models
{
    public static class ServicioGemini
    {
        private static readonly string ApiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
        private static readonly string Url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";
        public static async Task<string> GenerarTexto(string prompt)
        {
            using (var client = new HttpClient())
            {
                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(Url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Error al llamar a Gemini: " + responseString);

                var jObj = JObject.Parse(responseString);
                string texto = jObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                return texto?.Trim() ?? "";
            }
        }
    }
}