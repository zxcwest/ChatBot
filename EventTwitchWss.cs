namespace TwitchChatBot
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Http;

    class EventTwitchWss
    {
        private ClientWebSocket ws = null!;
        private string sessionId = string.Empty;

        public event Action<string>? OnNewFollower;
        public event Action<string>? OnStreamOnline;
        public event Action<string, int>? OnRaidChanel;

        // Метод для подключения, принимает логин канала (не id)
        public async Task ConnectAsync(string clientId, string oauthToken, string broadcasterLogin)
        {
            // Получаем user_id по логину канала
            string broadcasterId = await GetUserIdByLogin(broadcasterLogin, clientId, oauthToken);
            if (string.IsNullOrEmpty(broadcasterId))
            {
                return;
            }

            ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("wss://eventsub.wss.twitch.tv/ws"), CancellationToken.None);


            // Запускаем получение сообщений в отдельной задаче
            _ = Task.Run(() => ReceiveLoop(clientId, oauthToken, broadcasterId));
        }

        private async Task ReceiveLoop(string clientId, string oauthToken, string broadcasterId)
        {
            var buffer = new byte[8192];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string type = root.GetProperty("metadata").GetProperty("message_type").GetString() ?? "";

                if (type == "session_welcome")
                {
                    sessionId = root.GetProperty("payload").GetProperty("session").GetProperty("id").GetString() ?? "";

                    // Подписываемся на события
                    await SubscribeToFollow(clientId, oauthToken, broadcasterId);
                    await SubscribeToStreamOnline(clientId, oauthToken, broadcasterId);
                    await SubscribeToRaid(clientId, oauthToken, broadcasterId);
                }
                else if (type == "notification")
                {
                    string eventType = root.GetProperty("metadata").GetProperty("subscription_type").GetString() ?? "";

                    if (eventType == "channel.follow")
                    {
                        var follower = root.GetProperty("payload").GetProperty("event").GetProperty("user_name").GetString() ?? "";
                        OnNewFollower?.Invoke(follower);
                    }
                    else if (eventType == "stream.online")
                    {
                        OnStreamOnline?.Invoke(broadcasterId);
                    }
                    else if (eventType == "channel.raid")
                    {
                        var payload = root.GetProperty("payload").GetProperty("event");
                        var fromName = payload.GetProperty("from_broadcaster_user_name").GetString() ?? "";
                        var viewers = payload.GetProperty("viewers").GetInt32();
                        OnRaidChanel?.Invoke(fromName, viewers);
                    }
                }
                else if (type == "session_keepalive")
                {
                    // Можно логировать, что сессия жива, если нужно
                }
                else if (type == "session_reconnect")
                {
                    Console.WriteLine("[Info] Twitch попросил переподключиться");
                    // Реализуй переподключение, если хочешь
                }
            }
        }

        private async Task SubscribeToFollow(string clientId, string oauthToken, string broadcasterId)
        {
            await SendSubscription(clientId, oauthToken, new
            {
                type = "channel.follow",
                version = "2",
                condition = new { broadcaster_user_id = broadcasterId, moderator_user_id = broadcasterId },
                transport = new { method = "websocket", session_id = sessionId }
            }, "channel.follow");
        }

        private async Task SubscribeToStreamOnline(string clientId, string oauthToken, string broadcasterId)
        {
            await SendSubscription(clientId, oauthToken, new
            {
                type = "stream.online",
                version = "1",
                condition = new { broadcaster_user_id = broadcasterId },
                transport = new { method = "websocket", session_id = sessionId }
            }, "stream.online");
        }

        private async Task SubscribeToRaid(string clientId, string oauthToken, string broadcasterId)
        {
            await SendSubscription(clientId, oauthToken, new
            {
                type = "channel.raid",
                version = "1",
                condition = new { to_broadcaster_user_id = broadcasterId },
                transport = new { method = "websocket", session_id = sessionId }
            }, "channel.raid");
        }

        private async Task SendSubscription(string clientId, string oauthToken, object data, string subName)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Clear();
            http.DefaultRequestHeaders.Add("Client-ID", clientId);
            http.DefaultRequestHeaders.Add("Authorization", oauthToken.StartsWith("Bearer ") ? oauthToken : $"Bearer {oauthToken}");

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);

            string body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Subscribe {subName}] Status: {response.StatusCode}, Body: {body}");

            // вот сюда вставляем проверку
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                Console.WriteLine($"[Warning] Подписка {subName} НЕ принята Twitch! Проверь токен и права (user:read:follows).");
            }
            else
            {
                Console.WriteLine($"[Info] Подписка {subName} успешно создана.");
            }
        }


        private async Task<string> GetUserIdByLogin(string login, string clientId, string token)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Clear();
            http.DefaultRequestHeaders.Add("Client-ID", clientId);
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await http.GetAsync($"https://api.twitch.tv/helix/users?login={login}");
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("[User Info] " + json);

            if (!response.IsSuccessStatusCode) return string.Empty;

            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            if (data.GetArrayLength() == 0) return string.Empty;

            return data[0].GetProperty("id").GetString() ?? string.Empty;
        }

    }
}
