using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ParallelAnalyzer.Core.Services
{
    /// <summary>
    /// Отвечает за поиск и загрузку пользовательских задач (плагинов),
    /// унаследованных от ParallelPatternsBase<T>.
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>
        /// Загружает все плагины из указанной папки (например, "Plugins").
        /// </summary>
        public static List<Type> LoadPlugins(string folderPath)
        {
            var foundPlugins = new List<Type>();

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Папка с плагинами не найдена: {folderPath}");
                return foundPlugins;
            }

            foreach (string dllPath in Directory.GetFiles(folderPath, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    var types = assembly.GetTypes()
                        .Where(t => !t.IsAbstract)
                        .Where(t => t.BaseType != null &&
                                    t.BaseType.IsGenericType &&
                                    t.BaseType.GetGenericTypeDefinition().Name == "ParallelPatternsBase`1")
                        .ToList();

                    foundPlugins.AddRange(types);

                    foreach (var type in types)
                        Console.WriteLine($"Найден плагин: {type.FullName} ({Path.GetFileName(dllPath)})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке {dllPath}: {ex.Message}");
                }
            }

            return foundPlugins;
        }
    }
}
