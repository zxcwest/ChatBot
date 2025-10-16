using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitchChatBot
{
    class Translator
    {
        private static bool ShouldSkipTranslation(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;

            string trimmed = text.Trim();

            // Только символы, числа, знаки препинания, пробелы
            if (Regex.IsMatch(trimmed, @"^[\p{P}\p{S}\d\s]+$"))
                return true;

            // Нет нормальных слов, только аббревиатуры
            if (!Regex.IsMatch(trimmed, @"\p{L}{2,}"))
                return true;

            // Очень короткие строки
            if (trimmed.Length <= 2)
                return true;

            // Коды и ключи вида ABC-123
            if (!trimmed.Contains(' ') && Regex.IsMatch(trimmed, @"^[A-Z0-9\-]{3,}$", RegexOptions.IgnoreCase))
                return true;

            // Один короткий "токен", часто смайлы
            if (!trimmed.Contains(' ') && Regex.IsMatch(trimmed, @"^[a-zA-Z]{2,10}$"))
                return true;

            return false;
        }

        private static async Task DetectAndTranslate(string inputText, string targetLang, string name)
        {
            if (ShouldSkipTranslation(inputText))
                return;

            using var client = new HttpClient();

            // Попытка определить язык
            string detectUrl = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(inputText)}&langpair=en|{targetLang}";
            HttpResponseMessage detectResp = await client.GetAsync(detectUrl);

            if (!detectResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка определения языка]");
                return;
            }

            string detectJson = await detectResp.Content.ReadAsStringAsync();
            using JsonDocument detectDoc = JsonDocument.Parse(detectJson);

            string detectedLang = "en";

            if (detectDoc.RootElement.TryGetProperty("langDetection", out JsonElement langDetection))
            {
                detectedLang = langDetection.GetProperty("detectedLang").GetString() ?? "en";
            }

            // Проверка на кириллицу
            if (detectedLang == "en" && Regex.IsMatch(inputText, @"\p{IsCyrillic}"))
            {
                detectedLang = "ru";
            }

            // Совпадение языков — не переводим
            if (detectedLang == targetLang)
            {
                Console.WriteLine($"{name}:[{detectedLang} → {targetLang}] {inputText} (язык совпадает)");
                return;
            }

            // Перевод
            string translateUrl = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(inputText)}&langpair={detectedLang}|{targetLang}";
            HttpResponseMessage translateResp = await client.GetAsync(translateUrl);

            if (!translateResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка перевода]");
                return;
            }

            string translateJson = await translateResp.Content.ReadAsStringAsync();
            using JsonDocument transDoc = JsonDocument.Parse(translateJson);

            string translatedText = transDoc.RootElement.GetProperty("responseData").GetProperty("translatedText").GetString();

            TwitchClientContainer.SendMessage($"{name}:[{detectedLang} → {targetLang}] {translatedText}");
        }

        public static Task DetectAndTranslateSave(string text, string targetLang, string name)
        {
            return DetectAndTranslate(text, targetLang, name);
        }
    }

    public class TranslationQueue
    {
        private readonly ConcurrentQueue<(string Text, string TargetLang, string Name)> _queue = new();
        private bool _isProcessing = false;

        public void Enqueue(string text, string targetLang, string name)
        {
            _queue.Enqueue((text, targetLang, name));
            ProcessQueue();
        }

        private async void ProcessQueue()
        {
            if (_isProcessing) return;

            _isProcessing = true;

            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    await Translator.DetectAndTranslateSave(item.Text, item.TargetLang, item.Name);
                    await Task.Delay(150); // Пауза между запросами
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка очереди перевода: {ex.Message}]");
                }
            }

            _isProcessing = false;
        }
    }
}
