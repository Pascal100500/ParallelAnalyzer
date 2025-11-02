using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ParallelAnalyzer.Core.Models;

namespace ParallelAnalyzer.Core.Data
{
    public class BenchmarkDbContext : DbContext
    {
        static BenchmarkDbContext()
        {
            using var db = new BenchmarkDbContext(skipEnsure: true);
            db.Database.EnsureCreated();
        }

        public BenchmarkDbContext(bool skipEnsure = false)
        {
            // пустой параметр для подавления вызова EnsureCreated при создании из статического конструктора
        }
        public DbSet<SystemInfo> Systems { get; set; }              
        public DbSet<NumberBenchmarkResult> NumberBenchmarks { get; set; } 
        public DbSet<FileBenchmarkResult> FileBenchmarks { get; set; }
        public DbSet<BenchmarkTask> Tasks { get; set; } = null!;
        public DbSet<BenchmarkSession> Sessions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Путь: <папка исполняемого файла>\Data\benchmark.db
            var dbFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dbFolder);

            var dbPath = Path.Combine(dbFolder, "benchmark.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
