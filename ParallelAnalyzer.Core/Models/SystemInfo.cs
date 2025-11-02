using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Models
{
    public class SystemInfo
    {
        public int Id { get; set; }
        public string MachineName { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string ProcessorName { get; set; } = "";
        public int LogicalCores { get; set; }
        public double RAM_GB { get; set; }
        public DateTime TestDate { get; set; } = DateTime.Now;

        public ICollection<NumberBenchmarkResult> Results { get; set; } = new List<NumberBenchmarkResult>();
        public ICollection<FileBenchmarkResult> FileResults { get; set; } = new List<FileBenchmarkResult>();
    }
}
