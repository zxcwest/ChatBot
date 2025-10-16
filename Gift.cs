namespace TwitchChatBot
{
    internal class Gift
    {
        public Gift()
        {
            Console.WriteLine("Модуль Gift успешно подключён");
        }
        public bool giftStart = false;
        public List<string> namesUser = new List<string>();

        public void AddUser(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = name.Trim().ToLower();

            foreach (var user in namesUser)
                if (user == name)
                    return;

            namesUser.Add(name);
        }
        public void RemoveUserList() => namesUser.Clear();
        public string UserVictori()
        {
            if (namesUser.Count == 0)
                return "участников нет";

            Random random = new Random();
            int number = random.Next(namesUser.Count);
            return $"Победитель: {namesUser[number]}! Поздравим его!";
        }

    }
}
