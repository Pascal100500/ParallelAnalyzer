using ParallelAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Tasks
{
    /// <summary>
    /// Задача: поиск простых чисел в массиве.
    /// Наследуется от базового шаблона ParallelPatternsBase<int>.
    /// </summary>
    public class PrimeNumbersTask : ParallelPatternsBase<int>
    {
        public int ElementsCount => N;
        public int MaxRandomValue => MaxValue;

        private readonly int N;         // количество элементов массива
        private readonly int MaxValue;  // диапазон случайных чисел

        public PrimeNumbersTask(int? n = null, int maxValue = 10_000_000)
        {
            N = n ?? TaskConfig.ArraySize; // ⬅️ теперь берем значение из UI
            MaxValue = maxValue;
        }

        
        public override void Setup()
        {
            data = Auxiliary.GenerateRandomArray(N, 2, MaxValue);
            if (N <= 0 || MaxValue < 2)
                throw new ArgumentException("Недопустимые параметры генерации массива.");
        }

        /// <summary>
        /// Проверка условия для каждого элемента (простое ли число).
        /// </summary>
        protected override bool CheckCondition(int number)
        {
            return Auxiliary.IsPrime(number);
        }

        public override string ToString()
        {
            return $"Поиск простых чисел в массиве размером {N:N0}";
        }
    }
}
