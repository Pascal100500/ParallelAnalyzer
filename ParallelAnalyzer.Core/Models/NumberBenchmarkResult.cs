using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Models
{
    public class NumberBenchmarkResult
    {
        public int Id { get; set; }
        public int TaskId { get; set; }                
        public BenchmarkTask Task { get; set; } = null!; 

        public int SystemInfoId { get; set; }
        public SystemInfo SystemInfo { get; set; } = null!;
        public int? SessionId { get; set; }
        public BenchmarkSession? Session { get; set; }

        public string MethodName { get; set; } = string.Empty;
        public double MeanMs { get; set; }
        public double StdDevMs { get; set; }
        public int N { get; set; } // размер входного массива
        public string? Comment { get; set; }

    }
}
