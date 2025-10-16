using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;

namespace TwitchChatBot
{
    class Command
    {
        public Command()
        {
            Console.WriteLine("Модуль Команды успешно подключён");
        }
        //Таймаут
        private async Task TimeoutUser(string userId, int duration, string reasonPerson, TwitchAPI api, string streamerID, string botID)
        {
            try
            {
                var banRequest = new BanUserRequest
                {
                    UserId = userId,
                    Reason = reasonPerson,
                    Duration = duration
                };
                await api.Helix.Moderation.BanUserAsync(streamerID, botID, banRequest);
                Console.WriteLine($"Пользователь {userId} получил таймаут.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при таймауте пользователя: " + ex.Message);
            }
        }

        //Удаление сообщение
        private async Task DeleteMessage(string messageId, TwitchAPI api, string streamerID, string botID)
        {
            try
            {
                await api.Helix.Moderation.DeleteChatMessagesAsync(streamerID, botID, messageId);
                Console.WriteLine($"Сообщение {messageId} удалено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при удалении сообщения: " + ex.Message);
            }
        }

        //Бан 
        private async Task BanUser(string userId, TwitchAPI api, string streamerID, string botID)
        {
            try
            {
                var banRequest = new BanUserRequest
                {
                    UserId = userId,
                    Reason = "Нарушение правил",
                    Duration = null
                };

                await api.Helix.Moderation.BanUserAsync(streamerID, botID, banRequest);

                Console.WriteLine($"Пользователь {userId} забанен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при бане пользователя: " + ex.Message);
            }
        }

        //Смена титульника
        private async Task SetupTitleStream(string title, TwitchAPI api, string streamerID)
        {
            var requestTitlestream = new ModifyChannelInformationRequest()
            {
                Title = title
            };
            await api.Helix.Channels.ModifyChannelInformationAsync(streamerID, requestTitlestream);
        }
        //Смена игры
        private async Task SetupGameStream(string game, TwitchAPI api, string streamerID)
        {
            var requestGame = new ModifyChannelInformationRequest()
            {
                GameId = game
            };
            await api.Helix.Channels.ModifyChannelInformationAsync(streamerID, requestGame);
        }
        //Масс бан
        private async Task MassBanSetup(string message)
        {
            // Пример команды: "!масс бан, 90"
            string[] result = message.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (result.Length < 2)
            {
                TwitchClientContainer.SendMessage("Формат: !масс <слово>, <секунды>");
                return;
            }

            // Убираем "!масс" из первой части, оставляем только ключевое слово
            string wordPart = result[0].Replace("!масс", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (string.IsNullOrWhiteSpace(wordPart))
            {
                TwitchClientContainer.SendMessage("Не указано слово для бана.");
                return;
            }

            if (!int.TryParse(result[1].Trim(), out int seconds) || seconds <= 0)
            {
                TwitchClientContainer.SendMessage("Вторым аргументом должно быть положительное число секунд!");
                return;
            }

            // Активация масс-бана
            TwitchClientContainer.massBanActive = true;
            TwitchClientContainer.massWordBan = wordPart;
            TwitchClientContainer.massBanDuration = seconds;

            TwitchClientContainer.SendMessage($"Масс бан активирован по слову \"{wordPart}\" на {seconds} секунд.");

            // Деактивация по таймеру
            _ = Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000);
                TwitchClientContainer.massBanActive = false;
                TwitchClientContainer.SendMessage("Масс бан отключён автоматически.");
            });
        }

        //Сделать клип
        private async Task CreateClip(TwitchAPI api, string brodcasterID)
        {
            var clip = await api.Helix.Clips.CreateClipAsync(broadcasterId:brodcasterID);
            Console.WriteLine($"Клип сделан ссылка вот https://clips.twitch.tv/{clip.CreatedClips}");
        }

        //Безопастные методы
        //Таймаут
        public async void TimeotUserSafe(string userId, int duration, string reason, TwitchAPI api, string streamerID, string botID)
        {
            await TimeoutUser(userId, duration, reason, api, streamerID, botID);
        }
        //Удаление
        public async void DeleteMessageSafe(string messageId, TwitchAPI api, string streamerID, string botID)
        {
            await DeleteMessage(messageId, api, streamerID, botID);
        }
        //Бан
        public async void BanUserSafe(string userId, TwitchAPI api, string streamerID, string botID)
        {
            await BanUser(userId, api, streamerID, botID);
        }
        //Смена титула
        public async void SetupTitleStreamSave(string mesage, TwitchAPI api, string streamerID)
        {
            string clearMessage = mesage.Substring("!титл ".Length).Trim();
            await SetupTitleStream(clearMessage, api, streamerID);
        }
        //Смена игры сейф
        public async void SetupGameStreamSave(string message, TwitchAPI api, string streamerID)
        {
            string clearMessage = message.Substring("!game ".Length).Trim();
            await SetupGameStream(clearMessage, api, streamerID);
        }
        //Масс бан сейф
        public async void MassBanSetupSave(string message)
        {
            await MassBanSetup(message);
        }
        //Сделать клип
        public async void CreateClipSave(TwitchAPI api, string brodcasterID)
        {
            await CreateClip(api, brodcasterID);
        }
    }
}
