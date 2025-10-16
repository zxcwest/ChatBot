using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Polls.CreatePoll;

namespace TwitchChatBot
{
    class Poll
    {

        public Poll()
        {
            Console.WriteLine("Модуль Опросы подключён");
        }

        private async Task CreatePollAsync(string title, string[] variants, TwitchAPI api, string streamerID)
        {

            var choices = variants.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => new TwitchLib.Api.Helix.Models.Polls.CreatePoll.Choice { Title = v }).ToList();

            if (choices.Count < 2 || choices.Count > 5)
            {
                TwitchClientContainer.SendMessage("Требуется больше двух аргументов");
                return;
            }
            var pollRequest = new CreatePollRequest
            {
                BroadcasterId = streamerID,
                Title = title,
                Choices = choices.ToArray(),
                DurationSeconds = 60,

            };
            await api.Helix.Polls.CreatePollAsync(pollRequest);
        }

        public async void CreatePollAsyncSave(string message, TwitchAPI api, string streamerID)
        {
            string[] word = message.Split(",").Select(w => w.Trim()).ToArray();
            string title = word[0];
            string clearTitle = title.Substring("!опрос ".Length).Trim();
            await CreatePollAsync(clearTitle, word.Skip(1).ToArray(), api, streamerID);
        }
    }
}
