using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TwitchLib.Api;

namespace TwitchChatBot
{
    public class GptHandler
    {
        private readonly string apiKey;
        private readonly HttpClient httpClient;
        private Command command = new Command();
        private readonly string moderationPrompt;
        private SocialRaiting social;

        public GptHandler(string apiKey, SocialRaiting social)
        {
            this.apiKey = apiKey;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            moderationPrompt = GptLoader.LoadGptPrompt("gpt_prompt.json");

            Console.WriteLine("Модуль GPT подключён");
            this.social = social;
        }

        public async Task<string> AskAsync(string message)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();
        }

        public async Task<bool> IsToxicMessage(string message)
        {
            string filledPrompt = moderationPrompt.Replace("{message}", message);
            var answer = await AskAsync(filledPrompt);
            return answer.Trim().ToLower() == "да";
        }
        //GPT Метод для проверки сообщения на токсичность промт в GPTHandler
        private async Task HandleMessage(string user, string messageId, string message, bool itsVip, TwitchAPI api, string streamerID, string botID)
        {
            if (!await IsToxicMessage(message))
                return;

            Console.WriteLine($"[GPT] Сообщение {user} классифицировано как токсичное: \"{message}\"");

            // Можно удалить сообщение, если нужно
            // await command.DeleteMessageSafe(messageId, api, streamerID, botID);

            social.DecreaseRating(user.ToLower(), itsVip);
        }

        public async void HandleMessageSafe(string username, string messageId, string message, bool itsVip, TwitchAPI api, string streamerID, string botID)
        {
            await HandleMessage(username, messageId, message, itsVip, api, streamerID, botID);
        }


    }
}
