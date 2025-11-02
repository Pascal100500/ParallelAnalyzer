using ParallelAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Задача: разворот массива целых чисел.
    /// </summary>
    public class ArrayReverseTask : ParallelPatternsBase<int>
    {
        private readonly int N;
        private readonly int MaxValue;

        public ArrayReverseTask(int? n = null, int maxValue = 1_000_000)
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

        /// <summary>
        /// Проверка условия (не используется для разворота).
        /// Возвращает всегда true, чтобы пройти по всем элементам.
        /// </summary>
        protected override bool CheckCondition(int number) => true;

        // --- Методы реализации разворота массива ---

        public override int Sequential()
        {
            Array.Reverse(data!);
            return data!.Length;
        }

        public override int ParallelFor()
        {
            int len = data!.Length;
            int half = len / 2;

            Parallel.For(0, half, i =>
            {
                int temp = data[i];
                data[i] = data[len - i - 1];
                data[len - i - 1] = temp;
            });

            return len;
        }

        public override int PLinqQuery()
        {
            // PLINQ не ускоряет разворот, просто используем симуляцию
            data = data!.AsParallel().Reverse().ToArray();
            return data.Length;
        }

        public override int TasksByCores()
        {
            int cores = Environment.ProcessorCount;
            int len = data!.Length;
            int chunk = len / cores;
            var tasks = new Task[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = (c == cores - 1) ? len : start + chunk;

                tasks[c] = Task.Run(() =>
                {
                    Array.Reverse(data, start, end - start);
                });
            }

            Task.WaitAll(tasks);

            return len;
        }

        public override string ToString() => $"Разворот массива ({N:N0} элементов)";
    }
}
