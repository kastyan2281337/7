using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace httpsnewpat
{


    class Program
    {
        private static readonly string DebugFile = "news_parser.log";

        static void Main(string[] args)
        {
            SetupDebugOutputToFile();
            PrintHeader();

            try
            {
                Console.WriteLine("Запрос новостей с RIA.ru...");

                string html = GetContent();

                if (string.IsNullOrEmpty(html))
                {
                    Console.WriteLine("Не удалось получить данные с сайта.");
                    UseSampleData();
                }
                else
                {
                    ParseNews(html);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка: " + ex.Message);
                UseSampleData();
            }

            Console.WriteLine("\nНажмите любую клавишу для завершения...");
            Console.ReadKey();
        }

        public static string GetContent()
        {
            try
            {
                string url = "https://ria.ru/";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Timeout = 20000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Debug.WriteLine($"[GET] Успешно: {response.StatusCode} | Размер: {response.ContentLength}");
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET] Ошибка: {ex.Message}");
                Console.WriteLine("Ошибка при подключении: " + ex.Message);
                return null;
            }
        }

        public static void ParseNews(string htmlCode)
        {
            if (string.IsNullOrEmpty(htmlCode)) return;

            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);

            Console.ForegroundColor = ConsoleColor.Gray;
            Debug.WriteLine("[DEBUG] HTML загружен, длина: " + htmlCode.Length);
            Console.ResetColor();

            // Расширенный поиск - пробуем несколько стратегий
            var allLinks = html.DocumentNode.SelectNodes("//a[@href]");
            if (allLinks != null)
            {
                Debug.WriteLine("[DEBUG] Найдено ссылок: " + allLinks.Count);
            }

            List<NewsItem> news = new List<NewsItem>();

            // Стратегия 1: Ссылки с датой в URL (2026)
            var dateLinks = html.DocumentNode.SelectNodes("//a[contains(@href,'/2026')]");
            if (dateLinks != null)
            {
                foreach (var link in dateLinks.Take(20))
                {
                    string title = link.InnerText.Trim().CleanText();
                    if (title.Length > 20 && title.Length < 120)
                    {
                        string href = link.GetAttributeValue("href", "");
                        if (!href.StartsWith("http")) href = "https://ria.ru" + href;
                        news.Add(new NewsItem { title = title, link = href });
                    }
                }
            }

            // Стратегия 2: H1-H3 внутри ссылок
            if (news.Count < 8)
            {
                var hLinks = html.DocumentNode.SelectNodes("//h1/a | //h2/a | //h3/a");
                if (hLinks != null)
                {
                    foreach (var link in hLinks.Take(10))
                    {
                        string title = link.InnerText.Trim().CleanText();
                        if (title.Length > 15 && title.Length < 100)
                        {
                            string href = link.GetAttributeValue("href", "");
                            if (!href.StartsWith("http")) href = "https://ria.ru" + href;
                            news.Add(new NewsItem { title = title, link = href });
                        }
                    }
                }
            }

            // Стратегия 3: Любые длинные ссылки
            if (news.Count < 8)
            {
                var longLinks = html.DocumentNode.SelectNodes("//a");
                if (longLinks != null)
                {
                    foreach (var link in longLinks.Take(50))
                    {
                        string title = link.InnerText.Trim().CleanText();
                        if (title.Length > 25 && title.Length < 80 && !news.Any(n => n.title.Contains(title.Substring(0, 20))))
                        {
                            string href = link.GetAttributeValue("href", "");
                            if (href.Contains("/20") || href.Contains("/news") || href.Contains(".html"))
                            {
                                if (!href.StartsWith("http")) href = "https://ria.ru" + href;
                                news.Add(new NewsItem { title = title, link = href });
                            }
                        }
                    }
                }
            }

            if (news.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Новости не найдены - структура изменилась");
                Console.WriteLine("Проверьте debug_log.txt для диагностики");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"\nНайдено новостей: {news.Count}\n");

            foreach (var item in news.Take(12))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(new string('═', 90));
                Console.WriteLine(item.title);
                Console.WriteLine(new string('─', 90));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Ссылка: {item.link}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        static void UseSampleData()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nИспользуется пример данных:");
            Console.ResetColor();

            List<SampleNews> samples = new List<SampleNews>
            {
                new SampleNews { title = "Экономика России выросла на 4.1% в 2025 году", link = "https://ria.ru/20260122/ekonomika-1234567890.html" },
                new SampleNews { title = "Путин провел встречу с лидерами ЕАЭС", link = "https://ria.ru/20260122/eaes-0987654321.html" },
                new SampleNews { title = "Снегопад парализовал Москву", link = "https://ria.ru/20260122/pogoda-1122334455.html" },
                new SampleNews { title = "Россия запустила новый спутник", link = "https://ria.ru/20260122/kosmos-5566778899.html" }
            };

            foreach (var news in samples)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(new string('═', 90));
                Console.WriteLine(news.title);
                Console.WriteLine(new string('─', 90));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Ссылка: {news.link}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n" + new string('═', 90));
            Console.WriteLine("                       ПАРСЕР НОВОСТЕЙ RIA.RU");
            Console.WriteLine("                 Заголовки + Ссылки (DEBUG режим)");
            Console.WriteLine(new string('═', 90));
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void SetupDebugOutputToFile()
        {
            try
            {
                var listener = new TextWriterTraceListener(DebugFile);
                Debug.Listeners.Clear();
                Debug.Listeners.Add(listener);
                Debug.AutoFlush = true;
                Debug.WriteLine($"=== Парсинг RIA.ru | Запуск: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch { }
        }
    }

    public class NewsItem
    {
        public string title { get; set; }
        public string link { get; set; }
    }

    public class SampleNews
    {
        public string title { get; set; }
        public string link { get; set; }
    }

    public static class StringExtensions
    {
        public static string CleanText(this string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(Regex.Replace(text, @"\s+", " "),
                @"[^\w\s\u0400-\u04FF\u0020\u002C\u002E\u003A\u003B\u0021\u003F\u002D\u0028\u0029\u0022]", "").Trim();
        }
    }
}

