using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace ParallelAnalyzer.Core
{
    /// <summary>
    /// Базовый шаблон, включающий 13 параллельных паттернов (вариантов реализации).
    /// Поддерживает универсальный тип данных T.
    /// </summary>
    public abstract class ParallelPatternsBase<T>
    {
        protected T[]? data;

        /// <summary>
        /// Каждая конкретная задача должна подготовить свои данные.
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Проверка условия (например, IsPrime).
        /// </summary>
        protected abstract bool CheckCondition(T item);

        // 1️) Sequential
        public virtual int Sequential()
        {
            int count = 0;
            foreach (var x in data!)
                if (CheckCondition(x)) count++;
            return count;
        }

        // 2️) Parallel.For
        public virtual int ParallelFor()
        {
            int total = 0;
            Parallel.For(0, data!.Length,
                () => 0,
                (i, state, local) =>
                {
                    if (CheckCondition(data[i])) local++;
                    return local;
                },
                local => Interlocked.Add(ref total, local));
            return total;
        }

        // 3️) PLINQ Query
        public virtual int PLinqQuery()
        {
            return data!.AsParallel().Count(CheckCondition);
        }

        // 4️) TasksByCores — Task.Factory по количеству ядер
        public virtual int TasksByCores()
        {
            int cores = Environment.ProcessorCount;
            int chunk = data!.Length / cores;

            var tasks = new Task<int>[cores];
            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = (c == cores - 1) ? data.Length : start + chunk;
                tasks[c] = Task.Factory.StartNew(() =>
                {
                    int local = 0;
                    for (int i = start; i < end; i++)
                        if (CheckCondition(data[i])) local++;
                    return local;
                }, TaskCreationOptions.LongRunning);
            }

            Task.WaitAll(tasks);
            return tasks.Sum(t => t.Result);
        }

        // 5️) Parallel.ForEach
        public virtual int ParallelForEach()
        {
            int total = 0;
            object locker = new();

            Parallel.ForEach(data!, x =>
            {
                if (CheckCondition(x))
                {
                    lock (locker)
                        total++;
                }
            });

            return total;
        }

        // 6️) Parallel.Invoke
        public virtual int ParallelInvoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = data!.Length / cores;
            int total = 0;

            Parallel.Invoke(Enumerable.Range(0, cores).Select(c => (Action)(() =>
            {
                int start = c * chunk;
                int end = (c == cores - 1) ? data.Length : start + chunk;
                int local = 0;
                for (int i = start; i < end; i++)
                    if (CheckCondition(data[i])) local++;
                Interlocked.Add(ref total, local);
            })).ToArray());

            return total;
        }

        // 7️) Parallel.ForEach + ConcurrentBag
        public virtual int ParallelForEachConcurrentBag()
        {
            var bag = new ConcurrentBag<T>();
            Parallel.ForEach(data!, x =>
            {
                if (CheckCondition(x))
                    bag.Add(x);
            });
            return bag.Count;
        }

        // 8️) Parallel.For + List (каждый поток использует свой список)
        public virtual int ParallelForList()
        {
            List<T> allResults = new();
            object locker = new();

            Parallel.For(0, data!.Length,
                () => new List<T>(),
                (i, state, localList) =>
                {
                    if (CheckCondition(data[i]))
                        localList.Add(data[i]);
                    return localList;
                },
                localList =>
                {
                    lock (locker)
                        allResults.AddRange(localList);
                });

            return allResults.Count;
        }

        // 9️) ListProcessing (RemoveAll + Sort — имитация обработки списка)
        public virtual int ListProcessing()
        {
            var list = data!.Where(CheckCondition).ToList();
            list.RemoveAll(x => x == null);
            list.Sort();
            return list.Count;
        }

        // 10) TasksLists — каждый Task формирует свой список
        public virtual int TasksLists()
        {
            int procCount = Environment.ProcessorCount;
            int chunkSize = (int)Math.Ceiling((double)data!.Length / procCount);

            var tasks = new List<Task<List<T>>>();

            for (int i = 0; i < procCount; i++)
            {
                int start = i * chunkSize;
                int end = Math.Min(start + chunkSize, data.Length);

                tasks.Add(Task.Run(() =>
                {
                    List<T> localList = new();
                    for (int j = start; j < end; j++)
                        if (CheckCondition(data[j]))
                            localList.Add(data[j]);
                    return localList;
                }));
            }

            Task.WaitAll(tasks.ToArray());
            return tasks.Sum(t => t.Result.Count);
        }

        // 11️) ArrayPoolSimulated — использование буферов памяти
        public virtual int ArrayPoolSimulated()
        {
            var pool = ArrayPool<T>.Shared;
            int len = data!.Length;
            int total = 0;

            // Параллельный проход с thread-local состоянием: (buf, count)
            Parallel.For(0, len,
                // localInit: каждый поток арендует маленький буфер
                () => (buf: pool.Rent(1024), count: 0),

                // body: при необходимости расширяем локальный буфер
                (i, state, local) =>
                {
                    if (CheckCondition(data[i]))
                    {
                        if (local.count >= local.buf.Length)
                        {
                            // расширяем локальный буфер
                            T[] bigger = pool.Rent(local.buf.Length * 2);
                            Array.Copy(local.buf, bigger, local.count);
                            pool.Return(local.buf, clearArray: false);
                            local.buf = bigger;
                        }
                        local.buf[local.count++] = data[i];
                    }
                    return local;
                },

                // localFinally: добавляем локальный счётчик и возвращаем буфер
                local =>
                {
                    Interlocked.Add(ref total, local.count);
                    pool.Return(local.buf, clearArray: false);
                });

            return total;
        }

        // 12️) PLINQ WithDegree
        public virtual int PLinqWithDegree()
        {
            int degree = Math.Max(2, Environment.ProcessorCount - 1);
            return data!.AsParallel()
                        .WithDegreeOfParallelism(degree)
                        .Count(CheckCondition);
        }

        // 13️) Partitioner.ForEach
        public virtual int PartitionerForEach()
        {
            int total = 0;
            object locker = new();

            var partitioner = Partitioner.Create(0, data!.Length);

            Parallel.ForEach(partitioner, range =>
            {
                int local = 0;
                for (int i = range.Item1; i < range.Item2; i++)
                    if (CheckCondition(data[i])) local++;
                Interlocked.Add(ref total, local);
            });

            return total;
        }
    }
}

