using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParallelAnalyzer.Core.Data;
using ParallelAnalyzer.Core.Models;

namespace ParallelAnalyzer.Core.Data
{
    public static class DatabaseManager
    {
        public static void Initialize()
        {
            using var db = new BenchmarkDbContext();
            db.Database.EnsureCreated();
        }

        public static int SaveSystemInfo(SystemInfo system)
        {
            using var db = new BenchmarkDbContext();
            db.Systems.Add(system);
            db.SaveChanges();
            return system.Id;
        }

        public static void SaveNumberBenchmark(NumberBenchmarkResult result)
        {
            using var db = new BenchmarkDbContext();
            db.NumberBenchmarks.Add(result);
            db.SaveChanges();
        }

        public static void SaveFileBenchmark(FileBenchmarkResult result)
        {
            using var db = new BenchmarkDbContext();
            db.FileBenchmarks.Add(result);
            db.SaveChanges();
        }
    }
}
