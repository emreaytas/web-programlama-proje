using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace webprogbackend.Services
{
    public class DeepService
    {
        // Basit bir örnek: gelen metni büyük harfe çevirip döner
        public async Task<string> ProcessTextAsync(string input)
        {
            // API adresini belirtin
            string apiUrl = "https://api.example.com/endpoint"; // Buraya gerçek API adresini yazın

            using var httpClient = new HttpClient();
            var requestBody = new { text = input };
            // JSON olarak POST isteği gönder
            var response = await httpClient.PostAsJsonAsync(apiUrl, requestBody);
            response.EnsureSuccessStatusCode();
            // API'den dönen cevabı string olarak al
            string result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
