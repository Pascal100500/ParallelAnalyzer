using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ParallelAnalyzer.Core.Config;
using ParallelAnalyzer.Core.Tasks;

namespace ParallelAnalyzer.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class PrimeNumbersBenchmark
    {
        [ParamsSource(nameof(ArraySizes))]
        public int N { get; set; }

        public static IEnumerable<int> ArraySizes => new[] { TaskConfig.ArraySize };

        private PrimeNumbersTask? task;

        [GlobalSetup]
        public void Setup()
        {
            task = new PrimeNumbersTask(N, 1_000_000);
            task.Setup();
        }

        [Benchmark(Baseline = true, Description = "Sequential")]
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

        [Benchmark(Description = "Parallel.For + List per Thread")]
        public int ParallelForList() => task!.ParallelForList();

        [Benchmark(Description = "ListProcessing")]
        public int ListProcessing() => task!.ListProcessing();

        [Benchmark(Description = "TasksLists")]
        public int TasksLists() => task!.TasksLists();

        [Benchmark(Description = "ArrayPoolSimulated")]
        public int ArrayPoolSimulated() => task!.ArrayPoolSimulated();

        [Benchmark(Description = "PLINQ With Degree")]
        public int PLinqWithDegree() => task!.PLinqWithDegree();

        [Benchmark(Description = "Partitioner.ForEach")]
        public int PartitionerForEach() => task!.PartitionerForEach();
    }
}
