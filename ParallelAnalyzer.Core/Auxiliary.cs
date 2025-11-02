using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core
{
    public static class Auxiliary
    {
        // генерация случайного массива
        public static int[] GenerateRandomArray(int length, int min = 2, int max = 1_000_000)
        {
            var rnd = new Random(12345);
            return Enumerable.Range(0, length).Select(_ => rnd.Next(min, max)).ToArray();
        }

        // простая проверка на простое число
        public static bool IsPrime(int number)
        {
            if (number < 2) return false;
            for (int i = 2; i <= Math.Sqrt(number); i++)
                if (number % i == 0)
                    return false;
            return true;
        }
    }
}
