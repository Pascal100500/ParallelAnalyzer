using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ParallelAnalyzer.Core.Config;
using ParallelAnalyzer.Core.Tasks;

namespace ParallelAnalyzer.Benchmark.Benchmarks
{
    /// <summary>
    /// Benchmark для задачи сортировки массива.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ArraySortBenchmark
    {
        private ArraySortTask? task;

        [GlobalSetup]

        public void Setup()
        {
            
            string? nValue = Environment.GetEnvironmentVariable("BENCHMARK_ARRAY_SIZE");
            int n = !string.IsNullOrEmpty(nValue) && int.TryParse(nValue, out int parsed)
                ? parsed
                : 1_000_000; 

           // Console.WriteLine($"[DEBUG] Запуск Benchmark: ArraySortBenchmark (N = {n:N0})");

            task = new ArraySortTask(n, 1_000_000);
            task.Setup();
        }
        //public void Setup()
        //{
        //    int n = TaskConfig.ArraySize; // Берём размер из UI
        //    Console.WriteLine($"[DEBUG] Запуск Benchmark: ArraySortBenchmark (N = {n:N0})");
        //    task = new ArraySortTask(n, 1_000_000);
        //    task.Setup();
        //}

        [Benchmark(Baseline = true, Description = "Sequential Sort")]
        public int Sequential() => task!.Sequential();

        [Benchmark(Description = "Parallel.For")]
        public int ParallelFor() => task!.ParallelFor();

        [Benchmark(Description = "Parallel.ForEach")]
        public int ParallelForEach() => task!.ParallelForEach();

        [Benchmark(Description = "Parallel.Invoke")]
        public int ParallelInvoke() => task!.ParallelInvoke();

        [Benchmark(Description = "PLINQ Query")]
        public int PLinqQuery() => task!.PLinqQuery();

        [Benchmark(Description = "Tasks by Cores")]
        public int TasksByCores() => task!.TasksByCores();

        [Benchmark(Description = "ListProcessing (RemoveAll + Sort)")]
        public int ListProcessing() => task!.ListProcessing();

        [Benchmark(Description = "TasksLists")]
        public int TasksLists() => task!.TasksLists();

        [Benchmark(Description = "ArrayPoolSimulated")]
        public int ArrayPoolSimulated() => task!.ArrayPoolSimulated();

        [Benchmark(Description = "Parallel.ForEach + ConcurrentBag")]
        public int ParallelForEachConcurrentBag() => task!.ParallelForEachConcurrentBag();

        [Benchmark(Description = "Parallel.For + List per thread")]
        public int ParallelForList() => task!.ParallelForList();

        [Benchmark(Description = "PLINQ WithDegreeOfParallelism")]
        public int PLinqWithDegree() => task!.PLinqWithDegree();

        [Benchmark(Description = "Partitioner.ForEach")]
        public int PartitionerForEach() => task!.PartitionerForEach();
    }
}


/*
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ParallelAnalyzer.Core.Config;
using ParallelAnalyzer.Core.Tasks;

namespace ParallelAnalyzer.Benchmark.Benchmarks
{
    /// <summary>
    /// Benchmark для задачи сортировки массива.
    /// Сравнивает производительность 13 параллельных паттернов.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ArraySortBenchmark
    {
        [ParamsSource(nameof(ArraySizes))]
        public int N { get; set; }

        public static IEnumerable<int> ArraySizes => new[] { TaskConfig.ArraySize };

        private ArraySortTask? task;

        [GlobalSetup]
        public void Setup()
        {
            task = new ArraySortTask(N, 1_000_000);
            task.Setup();
        }     

        [Benchmark(Baseline = true, Description = "Sequential Sort")]
        public int Sequential() => task!.Sequential();

        [Benchmark(Description = "Parallel.For")]
        public int ParallelFor() => task!.ParallelFor();

        [Benchmark(Description = "PLINQ Query")]
        public int PLinqQuery() => task!.PLinqQuery();

        [Benchmark(Description = "Tasks by Cores")]
        public int TasksByCores() => task!.TasksByCores();

        [Benchmark(Description = "Parallel.ForEach")]
        public int ParallelForEach() => task!.ParallelForEach();

        [Benchmark(Description = "Parallel.Invoke")]
        public int ParallelInvoke() => task!.ParallelInvoke();

        [Benchmark(Description = "Parallel.ForEach + ConcurrentBag")]
        public int ParallelForEachConcurrentBag() => task!.ParallelForEachConcurrentBag();

        [Benchmark(Description = "Parallel.For + List per thread")]
        public int ParallelForList() => task!.ParallelForList();

        [Benchmark(Description = "ListProcessing (RemoveAll + Sort)")]
        public int ListProcessing() => task!.ListProcessing();

        [Benchmark(Description = "TasksLists")]
        public int TasksLists() => task!.TasksLists();

        [Benchmark(Description = "ArrayPoolSimulated")]
        public int ArrayPoolSimulated() => task!.ArrayPoolSimulated();

        [Benchmark(Description = "PLINQ WithDegree")]
        public int PLinqWithDegree() => task!.PLinqWithDegree();

        [Benchmark(Description = "Partitioner.ForEach")]
        public int PartitionerForEach() => task!.PartitionerForEach();
    }
}
*/