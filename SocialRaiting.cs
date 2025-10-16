using MySql.Data.MySqlClient;
using System;

namespace TwitchChatBot
{
    public class SocialRaiting
    {
        private string tableName;
        private string connStr;

        public SocialRaiting(string nickNameTable, string sqlcon)
        {
            connStr = sqlcon;
            tableName = nickNameTable;
            CreateTable();
            Console.WriteLine("Модуль социальный рейтинг подключён");
        }

        private void CreateTable()
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string createTable = $@"
                CREATE TABLE IF NOT EXISTS `{tableName}` (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    score INT DEFAULT 0
                );";

            using var cmd = new MySqlCommand(createTable, conn);
            cmd.ExecuteNonQuery();
        }

        public void SetScore(string username, int value)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string query = $@"
                INSERT INTO `{tableName}` (username, score)
                VALUES (@username, @score)
                ON DUPLICATE KEY UPDATE score = @score;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", username.ToLower());
            cmd.Parameters.AddWithValue("@score", value);
            cmd.ExecuteNonQuery();
        }

        public int GetScore(string username)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string query = $@"SELECT score FROM `{tableName}` WHERE username = @username;";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", username.ToLower());
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public void AddScore(string username, int amount)
        {
            int current = GetScore(username);
            SetScore(username, current + amount);
        }

        public void DecreaseRating(string username, bool vip, int amount = 100)
        {
            AddScore(username, -amount);
            if (GetScore(username) <= -30000 && vip)
                TwitchClientContainer.SendMessage($"Сними випку с {username}");
        }

        public void IncreaseRating(string username, bool vip, int amount = 20)
        {
            AddScore(username, amount);
            if (GetScore(username) >= 40000 && !vip)
                TwitchClientContainer.SendMessage($"Выдайте випку {username}");
        }

        public void ShowRating(string username)
        {
            int score = GetScore(username);
            TwitchClientContainer.SendMessage($"{username} Твой рейтинг : {score}");
        }
    }
}
