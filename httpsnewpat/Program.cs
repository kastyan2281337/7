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
                        Debug.WriteLine($"[GET] Успешно: {response.StatusCode}");
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

                // Универсальный поиск новостей RIA.ru
                var newsNodes = html.DocumentNode.SelectNodes(
                    "//article | //div[contains(@class,'list') or contains(@class,'item') or contains(@class,'news')]//a | " +
                    "//h1 | //h2 | //h3[contains(@class,'title')] | //a[contains(@href,'/20') or contains(@href,'/news')]");

                if (newsNodes == null || newsNodes.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"\nНайдено новостей: {newsNodes.Count}\n");

                int count = 0;
                foreach (var node in newsNodes.Take(15))
                {
                    // Заголовок
                    var titleNode = node.SelectSingleNode(".//h1 | .//h2 | .//h3 | . | .//a");
                    string title = titleNode?.InnerText.Trim().CleanText() ?? "Без заголовка";

                    // Ссылка
                    string link = node.GetAttributeValue("href", "#");
                    if (!link.StartsWith("http"))
                        link = "https://ria.ru" + link;

                    // Время (если есть)
                    var timeNode = node.SelectSingleNode(".//*[@class='time'] | .//time | .//*[@class='date']");
                    string time = timeNode?.InnerText.Trim() ?? DateTime.Now.ToString("HH:mm");

                    if (title.Length > 5 && count < 15)
                    {
                        count++;

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(new string('═', 80));
                        Console.WriteLine($"{count:D2}. {title}");
                        Console.WriteLine(new string('─', 80));

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Источник: RIA.ru | Время: {time}");

                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"Ссылка: {link}");
                        Console.ResetColor();
                    }
                }

                SaveNewsToFile(newsNodes.Take(15).ToList());
            }

            static void SaveNewsToFile(List<HtmlNode> newsNodes)
            {
                try
                {
                    var csvLines = new List<string> { "Заголовок,Ссылка,Время" };

                    foreach (var node in newsNodes)
                    {
                        var title = node.InnerText.Trim().CleanText();
                        var link = node.GetAttributeValue("href", "");
                        if (!link.StartsWith("http")) link = "https://ria.ru" + link;

                        csvLines.Add($"\"{title.Replace("\"", "\"\"")}\",\"{link}\",\"{DateTime.Now:HH:mm}\"");
                    }

                    File.WriteAllLines("ria_news.csv", csvLines, System.Text.Encoding.UTF8);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Ошибка сохранения: " + ex.Message);
                }
            }

            static void UseSampleData()
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nИспользуется пример данных (офлайн режим):");
                Console.ResetColor();

                List<SampleNews> samples = new List<SampleNews>
            {
                new SampleNews { title = "Экономика России показала рост на 4.1%", time = "10:30", link = "https://ria.ru/20260122/ekonomika-123456.html" },
                new SampleNews { title = "Новый этап СВО завершен успешно", time = "09:15", link = "https://ria.ru/20260122/svo-789012.html" },
                new SampleNews { title = "Путин: Россия - лидер в технологиях", time = "08:45", link = "https://ria.ru/20260122/putin-345678.html" }
            };

                foreach (var news in samples)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('═', 80));
                    Console.WriteLine(news.title);
                    Console.WriteLine(new string('─', 80));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Время: {news.time}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Ссылка: {news.link}");
                    Console.ResetColor();
                }
            }

            static void PrintHeader()
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n" + new string('═', 80));
                Console.WriteLine("                    ПАРСЕР НОВОСТЕЙ RIA.RU");
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

        // Расширение для очистки текста
        public static class StringExtensions
        {
            public static string CleanText(this string text)
            {
                if (string.IsNullOrEmpty(text)) return "";
                return Regex.Replace(Regex.Replace(text, @"\s+", " "),
                    @"[^\w\s\u0400-\u04FF\u0020\u002C\u002E\u003A\u003B\u0021\u003F\u002D\u0028\u0029\u0022]", "").Trim();
            }
        }

        // Пример данных
        public class SampleNews
        {
            public string title { get; set; }
            public string time { get; set; }
            public string link { get; set; }
        }
    }

