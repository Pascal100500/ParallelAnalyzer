using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Анализирует текстовые файлы и определяет,
    /// какое значение (строка) встречается чаще всего.
    /// Возвращает максимальную частоту.
    /// </summary>
    public class FileFrequencyTask : ParallelPatternsBase<string>
    {
        private string[] filePaths = Array.Empty<string>();

        public FileFrequencyTask(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");
            filePaths = Directory.GetFiles(folderPath, "*.txt");
            Setup();
        }

        public override void Setup()
        {
            // Собираем все строки (обрезаем, приводим к нижнему регистру)
            data = filePaths
                .SelectMany(path => File.ReadLines(path))
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        protected override bool CheckCondition(string s) => !string.IsNullOrWhiteSpace(s);

        // === 1) Последовательно
        public override int Sequential()
        {
            var freq = new ConcurrentDictionary<string, int>();
            foreach (var s in data!)
            {
                if (CheckCondition(s))
                    freq.AddOrUpdate(s, 1, (_, v) => v + 1);
            }
            return freq.Count == 0 ? 0 : freq.Max(kv => kv.Value);
        }

        // === 2) Parallel.For
        public override int ParallelFor()
        {
            var bag = new ConcurrentDictionary<string, int>();
            Parallel.For(0, data!.Length, i =>
            {
                var s = data[i];
                if (CheckCondition(s))
                    bag.AddOrUpdate(s, 1, (_, v) => v + 1);
            });
            return bag.Count == 0 ? 0 : bag.Max(kv => kv.Value);
        }

        // === 3) PLINQ
        public override int PLinqQuery()
        {
            var query = data!.AsParallel()
                .Where(CheckCondition)
                .GroupBy(x => x)
                .Select(g => g.Count());
            return query.Any() ? query.Max() : 0;
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
                    var loc = new Dictionary<string, int>();
                    for (int i = start; i < end; i++)
                    {
                        var s = data[i];
                        if (!CheckCondition(s)) continue;
                        if (loc.TryGetValue(s, out var v)) loc[s] = v + 1;
                        else loc[s] = 1;
                    }
                    return loc;
                });
            }).ToArray();

            Task.WaitAll(tasks);
            var merged = new Dictionary<string, int>();
            foreach (var t in tasks)
                foreach (var kv in t.Result)
                    merged[kv.Key] = merged.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;

            return merged.Count == 0 ? 0 : merged.Max(kv => kv.Value);
        }

        // === 5) Parallel.ForEach
        public override int ParallelForEach()
        {
            var bag = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(data!, s =>
            {
                if (CheckCondition(s))
                    bag.AddOrUpdate(s, 1, (_, v) => v + 1);
            });
            return bag.Count == 0 ? 0 : bag.Max(kv => kv.Value);
        }

        // === 6) Parallel.Invoke
        public override int ParallelInvoke()
        {
            int cores = Math.Max(1, Environment.ProcessorCount);
            int chunk = (int)Math.Ceiling((double)data!.Length / cores);
            var parts = new Dictionary<string, int>[cores];

            var actions = Enumerable.Range(0, cores).Select(c => (Action)(() =>
            {
                int start = c * chunk;
                int end = Math.Min(data!.Length, start + chunk);
                var loc = new Dictionary<string, int>();
                for (int i = start; i < end; i++)
                {
                    var s = data![i];
                    if (!CheckCondition(s)) continue;
                    if (loc.TryGetValue(s, out var v)) loc[s] = v + 1;
                    else loc[s] = 1;
                }
                parts[c] = loc;
            })).ToArray();

            Parallel.Invoke(actions);

            var merged = new Dictionary<string, int>();
            foreach (var loc in parts)
                foreach (var kv in loc)
                    merged[kv.Key] = merged.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;

            return merged.Count == 0 ? 0 : merged.Max(kv => kv.Value);
        }

        // === 7) Parallel.ForEach + ConcurrentBag (используем ConcurrentDictionary вместо bag)
        public override int ParallelForEachConcurrentBag()
        {
            var dict = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(data!, s =>
            {
                if (CheckCondition(s))
                    dict.AddOrUpdate(s, 1, (_, v) => v + 1);
            });
            return dict.Count == 0 ? 0 : dict.Max(kv => kv.Value);
        }

        // === 8) Parallel.For + List per thread (эмулируем словари на поток)
        public override int ParallelForList()
        {
            var locals = new List<Dictionary<string, int>>();
            object locker = new();

            Parallel.For(0, data!.Length,
                () => new Dictionary<string, int>(),
                (i, state, loc) =>
                {
                    var s = data[i];
                    if (CheckCondition(s))
                    {
                        if (loc.TryGetValue(s, out var v)) loc[s] = v + 1;
                        else loc[s] = 1;
                    }
                    return loc;
                },
                loc =>
                {
                    lock (locker) locals.Add(loc);
                });

            var merged = new Dictionary<string, int>();
            foreach (var loc in locals)
                foreach (var kv in loc)
                    merged[kv.Key] = merged.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;

            return merged.Count == 0 ? 0 : merged.Max(kv => kv.Value);
        }

        // === 9) ListProcessing — для строк сортировка/RemoveAll не имеет смысла для частот,
        // но оставим совместимость: вернём количество уникальных строк (как отображаемая метрика).
        public override int ListProcessing()
        {
            var list = data!.Where(CheckCondition).ToList();
            list.RemoveAll(x => x is null);
            list.Sort(StringComparer.Ordinal);
            // Кол-во уникальных (не maxFreq). Это технический паттерн; в отчёте пометим соответствующе.
            return list.Distinct().Count();
        }

        // === 10) TasksLists — собираем уникальные локально, потом сливаем с подсчётом частот
        public override int TasksLists()
        {
            int cores = Math.Max(1, Environment.ProcessorCount);
            int chunk = (int)Math.Ceiling((double)data!.Length / cores);

            var tasks = Enumerable.Range(0, cores).Select(c =>
            {
                int start = c * chunk;
                int end = Math.Min(data.Length, start + chunk);
                return Task.Run(() =>
                {
                    var loc = new Dictionary<string, int>();
                    for (int i = start; i < end; i++)
                    {
                        var s = data[i];
                        if (!CheckCondition(s)) continue;
                        if (loc.TryGetValue(s, out var v)) loc[s] = v + 1;
                        else loc[s] = 1;
                    }
                    return loc;
                });
            }).ToArray();

            Task.WaitAll(tasks);
            var merged = new Dictionary<string, int>();
            foreach (var t in tasks)
                foreach (var kv in t.Result)
                    merged[kv.Key] = merged.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;

            return merged.Count == 0 ? 0 : merged.Max(kv => kv.Value);
        }

        // === 11) ArrayPoolSimulated — неэффективно для строк, но реализуем подсчёт через ConcurrentDictionary
        public override int ArrayPoolSimulated()
        {
            var dict = new ConcurrentDictionary<string, int>();
            Parallel.For(0, data!.Length, i =>
            {
                var s = data[i];
                if (CheckCondition(s))
                    dict.AddOrUpdate(s, 1, (_, v) => v + 1);
            });
            return dict.Count == 0 ? 0 : dict.Max(kv => kv.Value);
        }

        // === 12) PLINQ WithDegree
        public override int PLinqWithDegree()
        {
            int degree = Math.Max(2, Environment.ProcessorCount - 1);
            var q = data!.AsParallel()
                         .WithDegreeOfParallelism(degree)
                         .Where(CheckCondition)
                         .GroupBy(x => x)
                         .Select(g => g.Count());
            return q.Any() ? q.Max() : 0;
        }

        // === 13) Partitioner.ForEach
        public override int PartitionerForEach()
        {
            var dict = new ConcurrentDictionary<string, int>();
            var part = System.Collections.Concurrent.Partitioner.Create(0, data!.Length);

            Parallel.ForEach(part, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var s = data[i];
                    if (CheckCondition(s))
                        dict.AddOrUpdate(s, 1, (_, v) => v + 1);
                }
            });

            return dict.Count == 0 ? 0 : dict.Max(kv => kv.Value);
        }

        public string GetMostFrequentValue()
        {
            // Последовательный подсчёт — удобный быстрый геттер
            var freq = new Dictionary<string, int>();
            foreach (var s in data!)
            {
                if (!CheckCondition(s)) continue;
                if (freq.TryGetValue(s, out var v)) freq[s] = v + 1;
                else freq[s] = 1;
            }
            if (freq.Count == 0) return "—";
            return freq.OrderByDescending(kv => kv.Value).First().Key;
        }
    }
}
