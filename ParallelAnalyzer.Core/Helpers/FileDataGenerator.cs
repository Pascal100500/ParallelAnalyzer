using System;
using System.IO;
using System.Linq;

namespace ParallelAnalyzer.Core.Helpers
{
    public enum FileDataType
    {
        UserLogs,
        RandomNumbers
    }

    public static class FileDataGenerator
    {
        /// <summary>
        /// Генерирует тестовые файлы для анализа.
        /// </summary>
        /// <param name="type">Тип данных: логи пользователей или случайные числа.</param>
        /// <param name="fileCount">Количество файлов.</param>
        /// <param name="linesPerFile">Количество строк в каждом файле.</param>
        /// <param name="targetFolder">Папка для сохранения файлов. Если null — создаётся Data/Files.</param>
        public static void Generate(FileDataType type, int fileCount = 5, int linesPerFile = 200, string? targetFolder = null)
        {
            string baseFolder = targetFolder ?? Path.Combine(AppContext.BaseDirectory, "Data", "Files");
            Directory.CreateDirectory(baseFolder);

            Random rnd = new Random();

            for (int f = 1; f <= fileCount; f++)
            {
                string filePath = Path.Combine(baseFolder, $"data_{type}_{f}.txt");
                using var writer = new StreamWriter(filePath);

                switch (type)
                {
                    case FileDataType.UserLogs:
                        string[] users = { "user1", "user2", "user3", "user4", "user5" };
                        string[] pages = { "pageA", "pageB", "pageC", "pageD", "pageE" };
                        for (int i = 0; i < linesPerFile; i++)
                        {
                            string user = users[rnd.Next(users.Length)];
                            string page = pages[rnd.Next(pages.Length)];
                            writer.WriteLine($"{user} {page}");
                        }
                        break;

                    case FileDataType.RandomNumbers:
                        for (int i = 0; i < linesPerFile; i++)
                        {
                            int number = rnd.Next(1, 100);
                            writer.WriteLine(number);
                        }
                        break;
                }
            }

            Console.WriteLine($"Сгенерировано {fileCount} файлов ({type}) в папке:\n{baseFolder}");
        }
    }
}
