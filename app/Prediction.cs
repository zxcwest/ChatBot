using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;

namespace TwitchChatBot
{
    class Prediction
    {
        public Prediction()
        {
            Console.WriteLine("Модуль Прогнозы подключён");
        }

        //Прогноз
        private async Task CreatePredict(string title, int second, string[] vote, TwitchAPI api, string streamerID)
        {
            if (vote.Length < 2 || vote.Length > 10)
            {
                TwitchClientContainer.SendMessage("Twitch требует от 2 до 5 вариантов в голосовании.");
                return;
            }
            // Используем правильный тип для вариантов
            var outcomes = vote.Select(v => new TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome { Title = v }).ToArray();

            // Создаём Prediction
            var predictionResponse = new CreatePredictionRequest()
            {
                BroadcasterId = streamerID,
                Title = title,
                Outcomes = outcomes,
                PredictionWindowSeconds = second
            };
            await api.Helix.Predictions.CreatePredictionAsync(predictionResponse);

        }
        //Победа
        private async Task AcceptPredictGame(string winningOutcomeTitle, TwitchAPI api, string streamerID)
        {
            var predictionsResponse = await api.Helix.Predictions.GetPredictionsAsync(streamerID);

            var activePrediction = predictionsResponse.Data.FirstOrDefault();
            if (activePrediction == null)
                return;

            // Найдём исход по названию
            var winningOutcome = activePrediction.Outcomes
                .FirstOrDefault(o => o.Title.Equals(winningOutcomeTitle, StringComparison.OrdinalIgnoreCase));

            if (winningOutcome == null)
                return;

            await api.Helix.Predictions.EndPredictionAsync(
                broadcasterId: streamerID,
                id: activePrediction.Id,
                status: PredictionEndStatus.RESOLVED,
                winningOutcomeId: winningOutcome.Id
            );
        }
        //Вызов 
        public async void AcceptPredictGameSave(string idwin, TwitchAPI api, string streamerID)
        {
            string clearid = idwin.Substring("!победа ".Length).Trim();
            await AcceptPredictGame(clearid, api, streamerID);
        }
        public async void CreatePredictionSave(string message, TwitchAPI api, string streamerID)
        {
            string[] word = message.Split(",");
            if (word.Length < 3)
            {
                TwitchClientContainer.SendMessage("Формат: заголовок,время в секундах,вариант1,вариант2,...");

                return;
            }
            string title = word[0].Trim();
            if (!int.TryParse(word[1].Trim(), out int second))
            {
                TwitchClientContainer.SendMessage("Вторым аргументом должно быть число секунд!");
                return;
            }
            string clearTitle = title.Substring("!ставка ".Length).Trim();
            string[] vote = word.Skip(2).Select(w => w.Trim()).ToArray();

            await CreatePredict(clearTitle, second, vote, api, streamerID);
        }
    }
}
