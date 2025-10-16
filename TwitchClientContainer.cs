using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TwitchChatBot
{
    class TwitchClientContainer
    {
        public static TwitchClient client;
        public ConnectionCredentials credentials;
        public TwitchAPI apiModerator;
        public TwitchAPI apiBrodcaster;

        // Публичные поля для ввода имени и ключа
        public string botUsername;
        public string botOAuth;
        public string brodcasterOAuth;
        public string chanelTwitch;

        // Закрытые ключи и поля
        public string secretKey;
        public string clientID;
        //токен бота
        public string refreshTokenBot;
        //Токен стримера
        public string refreshTokenBrodcaster;
        //Open AI
        public string openAIKey;
        public string telegramUrl;
        public string discrodUrl;
        public string donatUrl;
        public string nowStream;
        public string sqlCon;

        //Основные модули
        private GptHandler gpt;
        private SocialRaiting social;
        private Command command;
        private Poll poll;
        private Prediction prediction;
        private Banword banword;
        private EventSystem eventSystem;
        private TaroCardFolter taro;
        private EventTwitchWss eventSubTwitch;
        private Gift gift;

        //Логические переменные
        public static DateTime lastRozyskTime = DateTime.MinValue;
        public static readonly TimeSpan rozyskCooldown = TimeSpan.FromMinutes(5);
        public static int wordNonsense = 0;
        public static int wordUnderstood = 0;
        //Масс бан 
        public static int massBanDuration;
        public static bool massBanActive = false;
        public static string massWordBan;

        private string streamerID;
        private string botID;
        //Инициализация токенов и прав доступа
        public async Task Initialize()
        {
            // Инициализация GPT

            // Инициализация Twitch API Модератор
            apiModerator = new TwitchAPI();
            apiModerator.Settings.Secret = secretKey;
            apiModerator.Settings.ClientId = clientID;
            apiModerator.Settings.AccessToken = botOAuth;
            //Стример
            apiBrodcaster = new TwitchAPI();
            apiBrodcaster.Settings.Secret = secretKey;
            apiBrodcaster.Settings.ClientId = clientID;
            apiBrodcaster.Settings.AccessToken = brodcasterOAuth;

            // Обновляем токен перед использованием API
            await RefreshTokenBot();

            // Создаём клиент TwitchLib.Client
            client = new TwitchClient();
            eventSubTwitch = new EventTwitchWss();

            // Создаём учётные данные Требуется только для бота
            credentials = new ConnectionCredentials(botUsername, "oauth:" + botOAuth);

            //Собственный самописный вызова
            eventSubTwitch.ConnectAsync(clientID, botOAuth, chanelTwitch);

            // Инициализируем клиента
            client.Initialize(credentials);

            // Подписываемся на события TwitchLib
            client.OnConnected += OnConnected;
            client.OnJoinedChannel += OnJoinChanel;
            client.OnMessageReceived += MessageReceived;
            client.OnChatCommandReceived += ChatCommand;
            client.OnLog += OnLog;

            //Подписка Моей библиотеки
            eventSubTwitch.OnNewFollower += EventTwitch_OnNewFollower;
            eventSubTwitch.OnStreamOnline += EventTwitch_OnStreamOnline;
            eventSubTwitch.OnRaidChanel += EventTwitch_OnRaidChanel;

            // Инилицилизация требуемых классов
            social = new SocialRaiting(chanelTwitch, sqlCon);
            gpt = new GptHandler(openAIKey, social);
            command = new Command();
            poll = new Poll();
            prediction = new Prediction();
            banword = new Banword("banwords.json");
            eventSystem = new EventSystem();
            taro = new TaroCardFolter();
            gift = new Gift();
            //Соеденение с твичом
            client.Connect();
            await SetupIdModAndBrodcaster(); //Инициализация айди стример + модератор
        }

        private void EventTwitch_OnRaidChanel(string from, int viwer)
        {
            SendMessage($"Спасибо за рейд от {from}, общее количество приведённых зрителей {viwer}! Cпс за +250 bb");
        }

        //Подписки на мои события
        private void EventTwitch_OnStreamOnline(string brodcaster)
        {
            SendMessage(nowStream);

        }

        private void EventTwitch_OnNewFollower(string name)
        {
            SendMessage($"Новый фоловер: {name}");
        }

        //Определение айди стримера и бота
        private async Task SetupIdModAndBrodcaster()
        {
            var users = await apiModerator.Helix.Users.GetUsersAsync(logins: new List<string> { botUsername, chanelTwitch });
            streamerID = users.Users.First(u => u.Login.Equals(chanelTwitch, StringComparison.OrdinalIgnoreCase)).Id;
            botID = users.Users.First(u => u.Login.Equals(botUsername, StringComparison.OrdinalIgnoreCase)).Id;
        }
        //Пересоздание токена
        public async Task RefreshTokenBot()
        {
            try
            {
                var refreshResult = await apiModerator.Auth.RefreshAuthTokenAsync(refreshTokenBot, secretKey, clientID);

                apiModerator.Settings.AccessToken = refreshResult.AccessToken;

                botOAuth = refreshResult.AccessToken; // обновляем токен бота тоже
                refreshTokenBot = refreshResult.RefreshToken;

                Console.WriteLine("Токен Бота обновлён .");

                var refreshResaultBrodcaster = await apiBrodcaster.Auth.RefreshAuthTokenAsync(refreshTokenBrodcaster, secretKey, clientID);

                apiBrodcaster.Settings.AccessToken = refreshResaultBrodcaster.AccessToken;

                brodcasterOAuth = refreshResaultBrodcaster.AccessToken;
                refreshTokenBrodcaster = refreshResaultBrodcaster.RefreshToken;

                Console.WriteLine("Токен Стримера обновлён.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка обновления токена: " + ex.Message);
            }
        }
        //Вызов команд
        private void ChatCommand(object? sender, OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText.ToLower())
            {

                case "tg":
                case "тг":
                    MassPost(e, telegramUrl);
                    break;
                case "ds":
                case "дискорд":
                    MassPost(e,discrodUrl);
                    break;
                case "donate":
                case "донат":
                    MassPost(e, donatUrl);
                    break;
                case "розыск":
                    eventSystem.FindEventSafe(streamerID, botID, apiModerator);
                    break;
                
                case "vanish":
                case "ваниш":
                    command.TimeotUserSafe(e.Command.ChatMessage.Username, 1, "Событие", apiModerator, streamerID, botID);
                    break;
                
                case "рейтинг":
                    social.ShowRating(e.Command.ChatMessage.DisplayName);
                    break;
                case "ставка":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        prediction.CreatePredictionSave(e.Command.ChatMessage.Message, apiBrodcaster, streamerID);
                    break;
                case "титл":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        command.SetupTitleStreamSave(e.Command.ChatMessage.Message, apiBrodcaster, streamerID);
                    break;
                case "game":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        command.SetupGameStreamSave(e.Command.ChatMessage.Message, apiBrodcaster, streamerID);
                    break;

                case "опрос":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        poll.CreatePollAsyncSave(e.Command.ChatMessage.Message, apiBrodcaster, streamerID);
                    break;
                case "победа":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        prediction.AcceptPredictGameSave(e.Command.ChatMessage.Message, apiBrodcaster, streamerID);
                    break;
                case "масс":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        command.MassBanSetupSave(e.Command.ChatMessage.Message);
                    break;
                case "клип":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsVip || e.Command.ChatMessage.IsModerator)
                        command.CreateClipSave(apiBrodcaster, streamerID);
                    break;
                case "таро":
                    TaroCard randomCard = taro.GetRandomCard();
                    SendMessage($"{e.Command.ChatMessage.Username} тебе выпал {randomCard.Name} она означает {randomCard.Description}");
                    break;
                case "gift":
                    if (e.Command.ChatMessage.IsBroadcaster)
                    {
                        gift.RemoveUserList();
                        SendMessage("Начался розыгрыш, напишите + в чат чтобы зарагестрироваться");
                        gift.giftStart = true;
                    }
                    break;

                case "stopgift":
                    if (e.Command.ChatMessage.IsBroadcaster)
                    {
                        string winner = gift.UserVictori();
                        SendMessage(winner);
                        gift.giftStart = false;
                        gift.RemoveUserList();
                    }
                    break;
                case "закреп":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        SendMessage(nowStream);
                    break;
                default:
                    break;
            }
        }

        private void MassPost(OnChatCommandReceivedArgs e, string url)
        {
            var parts = e.Command.ChatMessage.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool isModerator = e.Command.ChatMessage.IsModerator;

            int numberSpot = 1;

            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedNumber) && isModerator)
            {
                numberSpot = Math.Min(parsedNumber, 10);
            }

            for (int i = 0; i < numberSpot; i++)
                SendMessage(url);
        }

        //Обработчик сообщения
        private void MessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            string message = e.ChatMessage.Message;
            string username = e.ChatMessage.Username;
            string userId = e.ChatMessage.UserId;
            string messageId = e.ChatMessage.Id;
            var itsVip = e.ChatMessage.IsVip;

            Console.WriteLine($"Сообщение от {username} : {message}");
            //Пишем все разговоры в лог
            Tech.LogFileMessage(message, username);
            MassBan(e, userId);//Масс бан 
            if (gift.giftStart && message == "+")
                gift.AddUser(username);

            if (e.ChatMessage.IsModerator)
                return;
            //Отстранить за бан слова в списке ниже
            if (banword.ContainsBannedWord(message))
            {
                SendMessage("пипяу");
                command.DeleteMessageSafe(messageId, apiModerator, streamerID, botID);
                command.TimeotUserSafe(userId, 600, "Бан ворд", apiModerator, streamerID, botID);
                social.DecreaseRating(username, itsVip);//Забираю рейтинг
                return;
            }

            //GPT Метод
            gpt.HandleMessageSafe(username, messageId, message, itsVip, apiModerator, streamerID, botID);
            if (message == "!рейтинг")
                return;
            eventSystem.WordCheck(e.ChatMessage.Message);
            social.IncreaseRating(username.ToLower(), itsVip);
            //Translator.DetectAndTranslateSave(message, "ru", username);

        }

        private void MassBan(OnMessageReceivedArgs e, string username)
        {
            if (massBanActive && e.ChatMessage.Message.ToLower().Contains(massWordBan.ToLower()))
            {
                command.TimeotUserSafe(username, massBanDuration, "Масс бан", apiModerator, streamerID, botID);
                return;
            }
        }

        //Логирование
        private void OnLog(object? sender, OnLogArgs e)
        {
            Console.WriteLine($"Лог: {e.Data}");
        }
        //Что пишет бот при соединении
        private void OnJoinChanel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Бот {e.BotUsername} подключён к каналу {e.Channel}");
            //SendMessage($"Бот {e.BotUsername} подключён к каналу {e.Channel}");
        }
        private void OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            client.JoinChannel(chanelTwitch);
            Console.WriteLine($"Подключено к Twitch как {e.BotUsername}");
        }
        //Отправка сообщение ботом любого
        public static void SendMessage(string message)
        {
            if (client.JoinedChannels.Count > 0)
                client.SendMessage(client.JoinedChannels[0], message);
        }
        public void LoadConfig(string configPatch)
        {
            if (!File.Exists(configPatch))
                throw new FileNotFoundException("Файл конфигруации не найден");

            string json = File.ReadAllText(configPatch);
            var config = JsonSerializer.Deserialize<Config>(json);

            botUsername = config.botUserName;
            botOAuth = config.botOAuth;
            brodcasterOAuth = config.brodcasterOAuth;
            chanelTwitch = config.chanelTwitch;

            secretKey = config.secretKey;
            clientID = config.clientID;
            refreshTokenBot = config.refreshTokenBot;
            refreshTokenBrodcaster = config.refreshTokenBrodcaster;

            openAIKey = config.apiOpenAI;

            telegramUrl = config.telegram;
            discrodUrl = config.discord;
            donatUrl = config.donatSite;
            nowStream = config.nowStream;
            sqlCon = config.sql;
        }
    }
}
