using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Models
{
    public class BenchmarkTask
    {
        public int Id { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int InputSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // связи "один ко многим"
        public List<NumberBenchmarkResult> NumberResults { get; set; } = new();
        public List<FileBenchmarkResult> FileResults { get; set; } = new();
    }
}
