namespace TwitchChatBot
{
    internal class Program
    {

        public static TwitchClientContainer clientContainer = new TwitchClientContainer();
        static async Task Main(string[] args)
        {
            try
            {
                clientContainer.LoadConfig("config.json");
                await clientContainer.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации: {ex}");
            }

            while (true)
            {
                try
                {
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в главном цикле: {ex}");
                }
            }
        }

    }
}
