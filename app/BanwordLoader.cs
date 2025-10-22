using System.Text.Json;

namespace TwitchChatBot
{
    public static class BanwordLoader
    {
        public static List<string> LoadBanWords(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл с бан-словами не найден");

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<BanwordConfig>(json);

            return config?.banList ?? new List<string>();
        }
    }

}
