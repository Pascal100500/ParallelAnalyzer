using ParallelAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Задача: анализ массива на чередование чётных и нечётных чисел.
    /// Наследуется от ParallelPatternsBase<int>.
    /// </summary>
    public class ArrayAlternationTask : ParallelPatternsBase<int>
    {
        private readonly int N;
        private readonly int MaxValue;

        public ArrayAlternationTask(int? n = null, int maxValue = 1_000_000)
        {
            N = n ?? TaskConfig.ArraySize;
            MaxValue = maxValue;
        }

        /// <summary>
        /// Генерируем случайный массив чисел.
        /// </summary>
        public override void Setup()
        {
            data = Auxiliary.GenerateRandomArray(N, 1, MaxValue);
        }

        /// <summary>
        /// Проверка условия — элемент сам по себе допустим (здесь всегда true).
        /// Основная логика чередования реализована в методах Sequential и ParallelFor.
        /// </summary>
        protected override bool CheckCondition(int number)
        {
            // ВНИМАНИЕ Метод оставлен для совместимости с базовым классом.
            return true;
        }

        /// <summary>
        /// Последовательная проверка чередования чёт/нечёт.
        /// </summary>
        public override int Sequential()
        {
            if (data == null || data.Length < 2)
                return 0;

            int count = 0;
            for (int i = 1; i < data.Length; i++)
            {
                if ((data[i] % 2) != (data[i - 1] % 2))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Параллельная версия через Parallel.For.
        /// </summary>
        public override int ParallelFor()
        {
            if (data == null || data.Length < 2)
                return 0;

            int count = 0;
            object locker = new object();

            Parallel.For(1, data.Length, i =>
            {
                if ((data[i] % 2) != (data[i - 1] % 2))
                {
                    lock (locker)
                        count++;
                }
            });

            return count;
        }

        /// <summary>
        /// Версия через PLINQ.
        /// </summary>
        public override int PLinqQuery()
        {
            if (data == null || data.Length < 2)
                return 0;

            var count = Enumerable.Range(1, data.Length - 1)
                .AsParallel()
                .Count(i => (data[i] % 2) != (data[i - 1] % 2));

            return count;
        }

        public override string ToString()
        {
            return $"Проверка чередования чёт/нечёт ({N:N0} элементов)";
        }
    }
}
