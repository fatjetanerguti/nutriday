using System.Text;
using System.Text.Json;

namespace NutriDay.API.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Groq:ApiKey"] ?? "";
        }

        public async Task<NutritionResult> AnalyzeFoodAsync(string rawInput)
        {
            var prompt = $@"Analyze this food and return ONLY a JSON object, nothing else, no markdown, no backticks:
""{rawInput}""

Return exactly in this format:
{{
  ""foodName"": ""name of the food"",
  ""calories"": 150,
  ""protein"": 5.2,
  ""carbs"": 20.1,
  ""fat"": 3.4
}}";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                max_tokens = 200,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions", content);

            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errorEl))
            {
                throw new Exception($"Groq API error: {errorEl}");
            }

            var text = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            text = text.Trim();
            if (text.StartsWith("```"))
            {
                text = text.Replace("```json", "").Replace("```", "").Trim();
            }

            return JsonSerializer.Deserialize<NutritionResult>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new NutritionResult();
        }
    }

    public class NutritionResult
    {
        public string FoodName { get; set; } = "";
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
    }
}