using ParallelAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Задача: сортировка массива случайных чисел различными параллельными методами.
    /// </summary>
    public class ArraySortTask : ParallelPatternsBase<int>
    {
        private readonly int N;
        private readonly int MaxValue;

        public ArraySortTask(int? n = null, int maxValue = 1_000_000)
        {
            N = n ?? TaskConfig.ArraySize;
            MaxValue = maxValue;
        }

        /// <summary>
        /// Подготовка исходных данных.
        /// </summary>
        public override void Setup()
        {
            data = Auxiliary.GenerateRandomArray(N, 1, MaxValue);
        }

        protected override bool CheckCondition(int number) => true;

        // === Методы реализации сортировки ===

        /// <summary> Обычная последовательная сортировка. </summary>
        public override int Sequential()
        {
            Array.Sort(data!);
            return data!.Length;
        }

        /// <summary> Пример параллельной сортировки с разбиением массива. </summary>
        public override int ParallelFor()
        {
            int len = data!.Length;
            int cores = Environment.ProcessorCount;
            int chunk = len / cores;

            var parts = new int[cores][];
            var tasks = new Task[cores];

            // Сортировка кусков
            for (int i = 0; i < cores; i++)
            {
                int start = i * chunk;
                int end = (i == cores - 1) ? len : start + chunk;
                tasks[i] = Task.Run(() =>
                {
                    int[] segment = new ArraySegment<int>(data, start, end - start).ToArray();
                    Array.Sort(segment);
                    parts[i] = segment;
                });
            }

            Task.WaitAll(tasks);

            // Объединяем все куски в один массив
            data = parts.SelectMany(p => p).OrderBy(x => x).ToArray();
            return data.Length;
        }

        /// <summary> PLINQ сортировка. </summary>
        public override int PLinqQuery()
        {
            data = data!.AsParallel().OrderBy(x => x).ToArray();
            return data.Length;
        }

        /// <summary> Сортировка с помощью Task.Factory.StartNew по ядрам. </summary>
        public override int TasksByCores()
        {
            int len = data!.Length;
            int cores = Environment.ProcessorCount;
            int chunk = len / cores;

            var tasks = Enumerable.Range(0, cores).Select(c =>
                Task.Factory.StartNew(() =>
                {
                    int start = c * chunk;
                    int end = (c == cores - 1) ? len : start + chunk;
                    int[] local = new ArraySegment<int>(data, start, end - start).ToArray();
                    Array.Sort(local);
                    return local;
                })
            ).ToArray();

            Task.WaitAll(tasks);
            data = tasks.SelectMany(t => t.Result).OrderBy(x => x).ToArray();
            return data.Length;
        }

        public override string ToString() => $"Сортировка массива ({N:N0} элементов)";
    }
}
