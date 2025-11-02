using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ParallelAnalyzer.Core.Tasks;

namespace ParallelAnalyzer.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class FileDeduplicateBenchmark
    {
        [ParamsSource(nameof(Folders))]
        public string FolderPath { get; set; } = string.Empty;

        public static IEnumerable<string> Folders()
        {
            yield return System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Files");
        }

        private FileDeduplicateTask? task;

        [GlobalSetup]
        public void Setup() => task = new FileDeduplicateTask(FolderPath);

        [Benchmark(Baseline = true, Description = "Последовательный метод")]
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
