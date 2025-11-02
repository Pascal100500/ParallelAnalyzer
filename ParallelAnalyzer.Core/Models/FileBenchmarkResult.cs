using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelAnalyzer.Core.Models
{
    public class FileBenchmarkResult
    {
        public int Id { get; set; }

        public int SystemInfoId { get; set; } 
        public SystemInfo SystemInfo { get; set; } = null!; 

        public string MethodName { get; set; } = string.Empty;
        public double MeanMs { get; set; } 
        public double StdDevMs { get; set; } 
        public int FileCount { get; set; } // Количество анализируемых файлов
        public long TotalTextSize { get; set; } // Суммарный объём текста (в символах или байтах)
        public string? Comment { get; set; } 
    }
}
