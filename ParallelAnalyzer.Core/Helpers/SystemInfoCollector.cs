using ParallelAnalyzer.Core.Data;
using ParallelAnalyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Helpers
{
    public static class SystemInfoCollector
    {
        public static SystemInfo GetCurrent()
        {
            string machineName = Environment.MachineName;
            string osVersion = Environment.OSVersion.VersionString;
            int cores = Environment.ProcessorCount;
            string cpuName = GetCpuName();
            double ramGb = GetRamSizeInGb();

            using var db = new BenchmarkDbContext();
            var existing = db.Systems.FirstOrDefault(s =>
                s.MachineName == machineName &&
                s.ProcessorName == cpuName &&
                Math.Abs(s.RAM_GB - ramGb) < 0.1);

            if (existing != null)
                return existing;

            var sys = new SystemInfo
            {
                MachineName = machineName,
                OSVersion = osVersion,
                ProcessorName = cpuName,
                LogicalCores = cores,
                RAM_GB = ramGb,
                TestDate = DateTime.Now
            };

           return sys;
        }

        // Получение модели процессора
        private static string GetCpuName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
                foreach (var item in searcher.Get())
                    return item["Name"]?.ToString() ?? "Unknown CPU";
            }
            catch
            {
                return "Unknown CPU";
            }
            return "Unknown CPU";
        }

        // Получение общего объема RAM (кроссплатформенно)
        private static double GetRamSizeInGb()
        {
            try
            {
                // Для Windows
                if (OperatingSystem.IsWindows())
                {
                    using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        if (obj["TotalVisibleMemorySize"] is ulong memKb)
                            return Math.Round(memKb / 1024.0 / 1024.0, 2);
                    }
                }

                // Для Linux и macOS — читаем из /proc/meminfo
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    var memInfo = System.IO.File.ReadAllLines("/proc/meminfo");
                    var line = memInfo.FirstOrDefault(l => l.StartsWith("MemTotal"));
                    if (line != null)
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1 && double.TryParse(parts[1].Trim().Split(' ')[0], out double kb))
                            return Math.Round(kb / 1024.0 / 1024.0, 2);
                    }
                }
            }
            catch
            {
                // Если не удалось определить
            }
            return 0;
        }



        // ==========================================================
        //   ТЕСТОВЫЙ МЕТОД ДЛЯ ПРОВЕРКИ SystemInfoCollector
        // ==========================================================
#if DEBUG
        public static void Main(string[] args)
        {
            Console.WriteLine("🔍 Получаем информацию о системе...");
            var sys = GetCurrent();

            Console.WriteLine("\n=== Информация о системе ===");
            Console.WriteLine($"Имя машины:   {sys.MachineName}");
            Console.WriteLine($"ОС:           {sys.OSVersion}");
            Console.WriteLine($"Процессор:    {sys.ProcessorName}");
            Console.WriteLine($"Лог. ядер:    {sys.LogicalCores}");
            Console.WriteLine($"ОЗУ (ГБ):     {sys.RAM_GB}");
            Console.WriteLine($"Дата теста:   {sys.TestDate}");
            Console.WriteLine("\n Данные успешно записаны в базу данных benchmark.db");

            Console.WriteLine("\n(Эту часть можно закомментировать после проверки)");
        }
#endif
        // ==========================================================
    }
}
