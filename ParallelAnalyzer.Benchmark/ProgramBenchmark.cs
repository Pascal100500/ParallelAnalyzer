
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using ParallelAnalyzer.Benchmark.Benchmarks;
using ParallelAnalyzer.Core.Data;
using ParallelAnalyzer.Core.Helpers;
using ParallelAnalyzer.Core.Models;

namespace ParallelAnalyzer.Benchmark
{
    public class ProgramBenchmark
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== ParallelAnalyzer Benchmark ===\n");

            using (var db = new BenchmarkDbContext())
            {
                // Пересоздаём базу данных
                db.Database.EnsureDeleted();
                Console.WriteLine("Старая база данных удалена.");
                db.Database.EnsureCreated();
                Console.WriteLine("Новая база данных создана.\n");

                // Добавляем системную информацию
                var system = new SystemInfo
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестный процессор",
                    LogicalCores = Environment.ProcessorCount,
                    RAM_GB = Math.Round((double)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024 * 1024), 2),
                    TestDate = DateTime.Now
                };

                db.Systems.Add(system);
                db.SaveChanges();
                Console.WriteLine($"Текущая система: {system.ProcessorName}, {system.RAM_GB} ГБ ОЗУ, {system.OSVersion}");
                                                          
                var task = new BenchmarkTask
                {
                    TaskName = "Поиск простых чисел в массиве",
                    Description = "Сравнение 13 параллельных методов при проверке чисел на простоту",
                    InputSize = 5_000_000
                };
                db.Tasks.Add(task);
                db.SaveChanges();

                //Настройка BenchmarkDotNet
                var config = ManualConfig.CreateMinimumViable()
                    .AddExporter(CsvExporter.Default)
                    .AddExporter(HtmlExporter.Default)
                    .AddExporter(MarkdownExporter.GitHub);

                //Запуск Benchmark
                var summary = BenchmarkRunner.Run<PrimeNumbersBenchmark>(config);

                if (summary.Reports.Length == 0)
                {
                    Console.WriteLine("⚠️ Benchmark не вернул отчётов. Завершение программы.");
                    return;
                }

                Console.WriteLine("\n=== Тестирование завершено ===");
                Console.WriteLine("Файлы сохранены в BenchmarkDotNet.Artifacts.\n");

                // Сохраняем результаты в базу
                foreach (var report in summary.Reports)
                {
                    string methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                    double mean = report.ResultStatistics?.Mean / 1_000_000 ?? 0;
                    double stdDev = report.ResultStatistics?.StandardDeviation / 1_000_000 ?? 0;

                    db.NumberBenchmarks.Add(new NumberBenchmarkResult
                    {
                        SystemInfoId = system.Id,
                        TaskId = task.Id, // 🔹 обязательно
                        MethodName = methodName,
                        MeanMs = mean,
                        StdDevMs = stdDev,
                        N = task.InputSize,
                        Comment = "Результат BenchmarkDotNet"
                    });
                }

                db.SaveChanges();
                Console.WriteLine("Результаты успешно сохранены в базу данных.");

                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }
    }
}


