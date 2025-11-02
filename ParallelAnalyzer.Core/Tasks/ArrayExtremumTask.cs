using ParallelAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Задача: поиск локальных минимумов и максимумов в массиве.
    /// </summary>
    public class ArrayExtremumTask : ParallelPatternsBase<int>
    {
        private readonly int N;
        private readonly int MaxValue;

        public ArrayExtremumTask(int? n = null, int maxValue = 1_000_000)
        {
            N = n ?? TaskConfig.ArraySize;
            MaxValue = maxValue;
        }

        public override void Setup()
        {
            data = Auxiliary.GenerateRandomArray(N, 1, MaxValue);
        }

        protected override bool CheckCondition(int number) => true;

        // === Методы реализации ===

        /// <summary>
        /// Последовательный поиск количества локальных экстремумов.
        /// </summary>
        public override int Sequential()
        {
            int count = 0;
            for (int i = 1; i < data!.Length - 1; i++)
            {
                if ((data[i] > data[i - 1] && data[i] > data[i + 1]) ||
                    (data[i] < data[i - 1] && data[i] < data[i + 1]))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Parallel.For — распараллеливание по индексам.
        /// </summary>
        public override int ParallelFor()
        {
            int count = 0;
            object locker = new();

            Parallel.For(1, data!.Length - 1, i =>
            {
                if ((data[i] > data[i - 1] && data[i] > data[i + 1]) ||
                    (data[i] < data[i - 1] && data[i] < data[i + 1]))
                {
                    lock (locker)
                        count++;
                }
            });

            return count;
        }

        /// <summary>
        /// Parallel LINQ (PLINQ) — параллельная выборка экстремумов.
        /// </summary>
        public override int PLinqQuery()
        {
            return Enumerable.Range(1, data!.Length - 2)
                .AsParallel()
                .Count(i =>
                    (data[i] > data[i - 1] && data[i] > data[i + 1]) ||
                    (data[i] < data[i - 1] && data[i] < data[i + 1]));
        }

        /// <summary>
        /// Tasks по ядрам — делим массив на диапазоны и ищем экстремумы.
        /// </summary>
        public override int TasksByCores()
        {
            int len = data!.Length;
            int cores = Environment.ProcessorCount;
            int chunk = len / cores;
            int total = 0;
            var tasks = new Task<int>[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = Math.Max(1, c * chunk);
                int end = (c == cores - 1) ? len - 1 : start + chunk;
                tasks[c] = Task.Run(() =>
                {
                    int localCount = 0;
                    for (int i = start; i < end - 1; i++)
                    {
                        if ((data[i] > data[i - 1] && data[i] > data[i + 1]) ||
                            (data[i] < data[i - 1] && data[i] < data[i + 1]))
                            localCount++;
                    }
                    return localCount;
                });
            }

            Task.WaitAll(tasks);
            total = tasks.Sum(t => t.Result);
            return total;
        }

        public override string ToString() => $"Поиск локальных минимумов/максимумов ({N:N0} элементов)";
    }
}
