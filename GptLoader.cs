using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchChatBot
{
    public static class GptLoader
    {
        public static string LoadGptPrompt(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл с промтом не найден");

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<GptConfig>(json);

            if (config == null || string.IsNullOrWhiteSpace(config.promptGpt))
                throw new InvalidDataException("Промт в файле отсутствует или пуст");

            return config.promptGpt;
        }
    }

}
