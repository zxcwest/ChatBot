namespace TwitchChatBot
{
    class Tech
    {
        public static void LogFileMessage(string message, string username)
        {
            DateTime now = DateTime.UtcNow;
            string folder = "Data";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string timestamp = now.ToString("yyyy-MM-dd");
            string fileName = Path.Combine(folder, $"{timestamp}.txt");

            string logLine = $"[{now:HH:mm:ss}] От {username}: {message}";
            File.AppendAllText(fileName, logLine + Environment.NewLine);
        }

        //private static void LogMessage(string filePitch, string message) => File.AppendAllText(filePitch, message + Environment.NewLine);

    }
}
