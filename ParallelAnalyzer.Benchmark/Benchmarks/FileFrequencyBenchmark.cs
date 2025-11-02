using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ParallelAnalyzer.Core.Tasks;

namespace ParallelAnalyzer.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class FileFrequencyBenchmark
    {
        // Папка с .txt файлами
        [ParamsSource(nameof(Folders))]
        public string FolderPath { get; set; } = string.Empty;

        public static IEnumerable<string> Folders()
        {
             yield return System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Files");
        }

        private FileFrequencyTask? task;

        [GlobalSetup]
        public void Setup() => task = new FileFrequencyTask(FolderPath);

        [Benchmark(Baseline = true)]
        public int Sequential() => task!.Sequential();

        [Benchmark] public int ParallelFor() => task!.ParallelFor();
        [Benchmark] public int PLinqQuery() => task!.PLinqQuery();
        [Benchmark] public int TasksByCores() => task!.TasksByCores();
        [Benchmark] public int ParallelForEach() => task!.ParallelForEach();
        [Benchmark] public int ParallelInvoke() => task!.ParallelInvoke();
        [Benchmark(Description = "Parallel.ForEach + ConcurrentBag")] public int ParallelForEachConcurrentBag() => task!.ParallelForEachConcurrentBag();
        [Benchmark] public int ParallelForList() => task!.ParallelForList();
        [Benchmark(Description = "ListProcessing (unique count)")] public int ListProcessing() => task!.ListProcessing();
        [Benchmark] public int TasksLists() => task!.TasksLists();
        [Benchmark] public int ArrayPoolSimulated() => task!.ArrayPoolSimulated();
        [Benchmark] public int PLinqWithDegree() => task!.PLinqWithDegree();
        [Benchmark] public int PartitionerForEach() => task!.PartitionerForEach();
    }
}
