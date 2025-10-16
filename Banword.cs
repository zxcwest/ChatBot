using System.Text.RegularExpressions;

namespace TwitchChatBot
{
    class Banword
    {
        public List<string> banList;

        public Banword(string pathToJson)
        {
            banList = BanwordLoader.LoadBanWords(pathToJson);
            Console.WriteLine("Модуль Banword подключён. Загружено слов: " + banList.Count);
        }

        public bool ContainsBannedWord(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string lowerMessage = message.ToLower();

            var words = Regex.Matches(lowerMessage, @"\b[\p{L}\p{N}_]+\b")
                             .Cast<Match>()
                             .Select(m => m.Value)
                             .ToList();

            foreach (string word in words)
            {
                if (banList.Contains(word))
                    return true;
            }

            return false;
        }
    }

}
