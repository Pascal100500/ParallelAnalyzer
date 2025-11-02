using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Models
{
    public class BenchmarkSession
    {
        public int Id { get; set; }
        public int SystemInfoId { get; set; }
        public SystemInfo SystemInfo { get; set; } = null!;

        public int TaskId { get; set; }
        public BenchmarkTask Task { get; set; } = null!;

        public DateTime RunDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        public ICollection<NumberBenchmarkResult> Results { get; set; } = new List<NumberBenchmarkResult>();
    }
}
