using TwitchLib.Api;

namespace TwitchChatBot
{
    class EventSystem
    {
        public static int wordChek = 0;
        public static string lastWord;
        public EventSystem()
        {
            Console.WriteLine("Модуль Система Ивентов подключёна");
        }
        private Command command = new Command();

        private async Task WordEvent(string message)
        {
            var lower = message.ToLower();

            if (lower.Contains("пончик"))
            {

                TwitchClientContainer.wordUnderstood++;

                // Выводим сообщение, если слово сказано 1, 2, 3, 4 раза
                if (TwitchClientContainer.wordUnderstood == 1)
                    TwitchClientContainer.SendMessage($"Слово пончик было сказано {TwitchClientContainer.wordUnderstood} раз");
                else if (TwitchClientContainer.wordUnderstood > 1 && TwitchClientContainer.wordUnderstood < 5)
                    TwitchClientContainer.SendMessage($"Слово пончик было сказано {TwitchClientContainer.wordUnderstood} раза");
                else if (TwitchClientContainer.wordUnderstood >= 5)
                    TwitchClientContainer.SendMessage($"Слово пончик было сказано {TwitchClientContainer.wordUnderstood} раз");
            }

            if (lower.Contains("бредик"))
            {
                TwitchClientContainer.wordNonsense++;

                if (TwitchClientContainer.wordNonsense == 1)
                    TwitchClientContainer.SendMessage($"Слово бредик было сказано {TwitchClientContainer.wordNonsense} раз");
                else if (TwitchClientContainer.wordNonsense > 1 && TwitchClientContainer.wordNonsense < 5)
                    TwitchClientContainer.SendMessage($"Слово бредик было сказано {TwitchClientContainer.wordNonsense} раза");
                else if (TwitchClientContainer.wordNonsense >= 5)
                    TwitchClientContainer.SendMessage($"Слово бредик было сказано {TwitchClientContainer.wordNonsense} раз");
            }

            await Task.CompletedTask;//заглушка для того чтобы IDE не ругался если знаешь что делаешь убери
        }

        private async Task FindEvent(string broadcasterId, string moderatorId, TwitchAPI api)
        {
            if (DateTime.UtcNow - TwitchClientContainer.lastRozyskTime < TwitchClientContainer.rozyskCooldown)
            {
                TwitchClientContainer.SendMessage("Команда перезаряжается");
                return;
            }
            TwitchClientContainer.SendMessage("Сейчас я определю преступника");

            TwitchClientContainer.lastRozyskTime = DateTime.UtcNow;

            var chattersResponse = await api.Helix.Chat.GetChattersAsync(broadcasterId, moderatorId, first: 10, after: null);
            var chatters = chattersResponse.Data;

            var rdm = new Random();
            int randomIndex = rdm.Next(chatters.Count());

            string targetUser = chatters[randomIndex].UserLogin; 

            TwitchClientContainer.SendMessage($"Внимание! Ведётся розыск: @{targetUser}");
            command.TimeotUserSafe(targetUser, 60, "Ивент", api, broadcasterId, moderatorId);
        }
        public void WordCheck(string word)
        {
            if (word == null)
            {
                lastWord = null;
                wordChek = 0;
                return;
            }

            if (word == lastWord)
            {
                wordChek++;
            }
            else
            {
                if (wordChek >= 4) 
                {
                    TwitchClientContainer.SendMessage($"Стрик {wordChek} из {lastWord}");
                }
                wordChek = 1;
                lastWord = word;
            }
        }


        public async void WordEventSave(string mesage)
        {
            await WordEvent(mesage);
        }

        public async void FindEventSafe(string broadcasterId, string moderatorId, TwitchAPI api)
        {
            await FindEvent(broadcasterId, moderatorId, api);
        }
    }
}
