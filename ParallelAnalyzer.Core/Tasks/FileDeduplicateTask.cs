using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Удаление дубликатов чисел, записанных по одному в строке.
    /// Возвращает количество уникальных чисел (после удаления дублей).
    /// </summary>
    public class FileDeduplicateTask : ParallelPatternsBase<int>
    {
        private string[] filePaths = Array.Empty<string>();

        public FileDeduplicateTask(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");
            filePaths = Directory.GetFiles(folderPath, "*.txt");
            Setup();
        }

        public override void Setup()
        {
            // читаем все числа, игнорируя мусорные строки
            data = filePaths
                .SelectMany(path => File.ReadLines(path))
                .Select(s => int.TryParse(s.Trim(), out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();
        }

        protected override bool CheckCondition(int x) => true;

        // === 1) Последовательно
        public override int Sequential()
        {
            var set = new HashSet<int>(data!);
            return set.Count;
        }

        // === 2) Parallel.For (локальные HashSet -> merge)
        public override int ParallelFor()
        {
            var locals = new List<HashSet<int>>();
            object locker = new();

            Parallel.For(0, data!.Length,
                () => new HashSet<int>(),
                (i, state, loc) => { loc.Add(data[i]); return loc; },
                loc => { lock (locker) locals.Add(loc); });

            var merged = new HashSet<int>();
            foreach (var hs in locals) merged.UnionWith(hs);
            return merged.Count;
        }

        // === 3) PLINQ
        public override int PLinqQuery()
        {
            return data!.AsParallel().Distinct().Count();
        }

        // === 4) TasksByCores
        public override int TasksByCores()
        {
            int cores = Math.Max(1, Environment.ProcessorCount);
            int chunk = (int)Math.Ceiling((double)data!.Length / cores);

            var tasks = Enumerable.Range(0, cores).Select(c =>
            {
                int start = c * chunk;
                int end = Math.Min(data.Length, start + chunk);
                return Task.Run(() =>
                {
                    var hs = new HashSet<int>();
                    for (int i = start; i < end; i++) hs.Add(data[i]);
                    return hs;
                });
            }).ToArray();

            Task.WaitAll(tasks);
            var merged = new HashSet<int>();
            foreach (var t in tasks) merged.UnionWith(t.Result);
            return merged.Count;
        }

        public override string ToString() => $"Удаление дубликатов (уникальные числа) — всего строк: {data?.Length ?? 0}";
    }
}
