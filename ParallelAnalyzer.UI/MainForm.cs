using Eto.Forms;
using ScottPlot.Eto;
using BenchmarkDotNet.Running;
using System.IO;
using ParallelAnalyzer.Core.Helpers;
using ParallelAnalyzer.Benchmark.Benchmarks;
using ParallelAnalyzer.Core.Config;

// Алиасы для предотвращения конфликтов
using ELabel = Eto.Forms.Label;
using EOrientation = Eto.Forms.Orientation;
using EFont = Eto.Drawing.Font;
using EFontStyle = Eto.Drawing.FontStyle;
using EFontFamilies = Eto.Drawing.FontFamilies;
using ESize = Eto.Drawing.Size;
using EColors = Eto.Drawing.Colors;
using EColor = Eto.Drawing.Color;
using ScottPlot;



namespace ParallelAnalyzer.UI
{
    public class MainForm : Form
    {
        private readonly EtoPlot plot;
        private readonly ELabel lblResult;
        private readonly DropDown cbTask;
        private readonly DropDown cbCategory;
        private string? defaultSaveDir;
        private List<(string Method, double MeanMs, int N)> lastResults = new();
        private readonly DropDown cbArraySize;

        public MainForm()
        {
            Title = "Parallel Analyzer";
            ClientSize = new ESize(1000, 1000);

            // Элементы управления
            var btnRunBenchmark = new Button { Text = "Запустить Benchmark" };
            var btnSave = new Button { Text = "Сохранить график в JPG" };
           // var btnSelectDir = new Button { Text = "Выбрать папку для сохранения" };
            var btnShowDb = new Button { Text = "Показать результаты из базы" };
            var btnSaveToDb = new Button { Text = "Сохранить результат" };

            cbCategory = new DropDown { Items = { "Анализ числовых методов", "Анализ файловых методов" } };
            cbTask = new DropDown();

            var btnGenerateFiles = new Button { Text = "Сгенерировать текстовые файлы для теста" };
            btnGenerateFiles.Enabled = false;

            var cbArraySize = new DropDown
            {
                Items = { "1 000 000", "5 000 000", "10 000 000", "20 000 000" },
                SelectedIndex = 0,
                Enabled = false 
            };
            cbArraySize.SelectedIndexChanged += (s, e) =>
            {
                if (cbArraySize.SelectedValue != null &&
                    int.TryParse(cbArraySize.SelectedValue.ToString().Replace(" ", ""), out int parsed))
                {
                    TaskConfig.ArraySize = parsed; 
                }
            };

            cbCategory.SelectedIndexChanged += (s, e) =>
            {
                cbTask.Items.Clear();

                if (cbCategory.SelectedIndex == 0)
                {
                    cbTask.Items.Add("Поиск простых чисел");
                    cbTask.Items.Add("Чередование чёт/нечёт чисел");
                    cbTask.Items.Add("Разворот массива");
                    cbTask.Items.Add("Сортировка массива");
                    cbTask.Items.Add("Поиск локального минимума/максимума");
                    btnGenerateFiles.Enabled = false;
                    cbArraySize.Enabled = true;
                }
                else
                {
                    cbTask.Items.Add("Анализ часто встречающегося пользователя");
                    cbTask.Items.Add("Удаление дубликатов в файлах");
                    btnGenerateFiles.Enabled = true;
                    cbArraySize.Enabled = false;

                    TaskConfig.ArraySize = cbArraySize.SelectedIndex switch
                    {
                        0 => 1_000_000,
                        1 => 5_000_000,
                        2 => 10_000_000,
                        3 => 20_000_000,
                        _ => 1_000_000
                    };
                }

                cbTask.SelectedIndex = 0;
                btnGenerateFiles.Enabled = cbCategory.SelectedIndex == 1;
            };


            lblResult = new ELabel { Text = "Результаты появятся здесь", TextAlignment = TextAlignment.Center };
            plot = new EtoPlot { Size = new ESize(900, 600) };

            // События для элементов управления
            btnRunBenchmark.Click += async (s, e) =>
            {
                lblResult.Text = "Benchmark выполняется...";
                string category = cbCategory.SelectedValue?.ToString() ?? "";
                string task = cbTask.SelectedValue?.ToString() ?? "";

                await Task.Run(() =>
                {
                    try
                    {
                        RunBenchmark(category, task);
                    }
                    catch (Exception ex)
                    {
                        Application.Instance.Invoke(() =>
                        {
                            MessageBox.Show(this, $"Ошибка при запуске Benchmark:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK);
                            lblResult.Text = "Ошибка выполнения.";
                        });
                    }
                });
            };

            btnSave.Click += (s, e) => SavePlotAsJpg();
            //btnSelectDir.Click += (s, e) => ChooseDefaultDirectory();
            btnShowDb.Click += (s, e) =>
            {
                var win = new ResultsWindow();
                win.Show();
            };
            
            btnSaveToDb.Click += (s, e) => SaveResultsToDatabase();

            btnGenerateFiles.Click += (s, e) =>
            {
                string? selectedTask = cbTask.SelectedValue?.ToString();
                if (selectedTask == null)
                {
                    MessageBox.Show(this, "Сначала выберите задачу.", "Информация", MessageBoxButtons.OK);
                    return;
                }

                FileDataType type = selectedTask.Contains("пользователя")
                    ? FileDataType.UserLogs
                    : FileDataType.RandomNumbers;

                FileDataGenerator.Generate(type, fileCount: 5, linesPerFile: 200);
                lblResult.Text = $"Сгенерированы тестовые файлы для задачи: {selectedTask}";
            };

            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
    {
        new ELabel
        {
            Text = "Анализ параллельных методов",
            Font = new EFont(EFontFamilies.Sans, 14, EFontStyle.Bold)
        },
        new ELabel { Text = "Выберите тип анализа:" },
        new StackLayout
        {
            Orientation = EOrientation.Horizontal,
            Spacing = 5,
            Items = { cbCategory }
        },
        new ELabel { Text = "Выберите задачу:" },
        new StackLayout
        {
            Orientation = EOrientation.Horizontal,
            Spacing = 5,
            Items = { cbTask }
        },
        new ELabel { Text = "Выберите размер массива (для числовых задач):" },
        new StackLayout
        {
            Orientation = EOrientation.Horizontal,
            Spacing = 5,
            Items = { new ELabel { Text = "Размер массива N:" }, cbArraySize }
        },
        new StackLayout
        {
            Orientation = EOrientation.Horizontal,
            Spacing = 5,
            Items = { btnGenerateFiles }
        },
        new StackLayout
        {
            Orientation = EOrientation.Horizontal,
            Spacing = 5,
            Items = { btnRunBenchmark, btnSaveToDb, btnShowDb, btnSave }
        },
        HLine(),
        lblResult,
        plot
    }
            };
        }

        // Тонкая горизонтальная линия-разделитель
        private Control HLine() => new Panel
        {
            Height = 1,
            BackgroundColor = new EColor(EColors.Gray.R, EColors.Gray.G, EColors.Gray.B, 102)
        };

        // Диалог выбора папки для сохраннения картинки с графиком
        //private void ChooseDefaultDirectory()
        //{
        //    var dialog = new SelectFolderDialog { Title = "Выберите папку для сохранения графиков" };
        //    if (dialog.ShowDialog(this) == DialogResult.Ok)
        //    {
        //        defaultSaveDir = dialog.Directory;
        //        lblResult.Text = $"Папка по умолчанию: {defaultSaveDir}";
        //    }
        //}


        // Запуск BenchmarkDotNet
        private void RunBenchmark(string category, string task)
        {
            Type? benchmarkType = null;

            if (category.Contains("числов"))
            {
                benchmarkType = task switch
                {
                    "Поиск простых чисел" => typeof(PrimeNumbersBenchmark),
                    "Чередование чёт/нечёт чисел" => typeof(ArrayAlternationBenchmark),
                    "Разворот массива" => typeof(ArrayReverseBenchmark),
                    "Сортировка массива" => typeof(ArraySortBenchmark),
                    "Поиск локального минимума/максимума" => typeof(ArrayExtremumBenchmark),
                    _ => null
                };
            }
            else
            {
                benchmarkType = task switch
                {
                    "Анализ часто встречающегося пользователя" => typeof(FileFrequencyBenchmark),
                    "Удаление дубликатов в файлах" => typeof(FileDeduplicateBenchmark),
                    _ => null
                };
            }

            if (benchmarkType == null)
            {
                MessageBox.Show(this, "Выбранная задача пока не реализована.", "Информация", MessageBoxButtons.OK);
                return;
            }

            // Создание окна прогресса строго в UI-потоке
            BenchmarkProgressWindow progressWindow = null!;
            Application.Instance.Invoke(() =>
            {
                progressWindow = new BenchmarkProgressWindow();
                progressWindow.Show();
            });

            // Запуск Benchmark в фоне
            Task.Run(() =>
            {
                try
                {
                    progressWindow.UpdateStatus("Выполняется BenchmarkDotNet...");
                    Environment.SetEnvironmentVariable("BENCHMARK_ARRAY_SIZE", TaskConfig.ArraySize.ToString());

                    var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType);

                    progressWindow.UpdateStatus("Обработка результатов...");

                    var results = summary.Reports
                        .Select(r => new
                        {
                            Method = r.BenchmarkCase.Descriptor.WorkloadMethod.Name,
                            Mean = r.ResultStatistics?.Mean ?? 0
                        })
                        .OrderBy(r => r.Mean)
                        .ToList();

                    double[] means = results.Select(r => r.Mean / 1_000_000.0).ToArray();
                    string[] labels = results.Select(r => r.Method).ToArray();

                    Application.Instance.Invoke(() =>
                    {
                        plot.Plot.Clear();
                        plot.Plot.Add.Bars(means);
                        plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                            Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);

                        plot.Plot.Title($"Результаты Benchmark — {task}");
                        plot.Plot.YLabel("Время (мс)");
                        plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 75;
                        plot.Plot.Axes.Margins(bottom: 0.3);
                        plot.Refresh();

                        lblResult.Text = $"Benchmark завершён ({results.Count} методов).";
                    });

                    lastResults = results
                        .Select(r => (r.Method, r.Mean / 1_000_000.0, ParallelAnalyzer.Core.Config.TaskConfig.ArraySize))
                        .ToList();
                }
                catch (Exception ex)
                {
                    string details = ex.InnerException?.Message ?? ex.Message;
                    Application.Instance.Invoke(() =>
                    {
                        MessageBox.Show(this, $"Ошибка при запуске Benchmark:\n{details}",
                            "Ошибка", MessageBoxButtons.OK);
                        lblResult.Text = "Ошибка выполнения.";
                    });
                }
                finally
                {
                    //Закрываем окно прогресса в UI-потоке
                    Application.Instance.Invoke(() => progressWindow.Close());
                }
            });
        }
        
        // Сохранение графика в JPG
        private void SavePlotAsJpg()
        {
            string fileName = $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

            if (!string.IsNullOrEmpty(defaultSaveDir))
            {
                string path = Path.Combine(defaultSaveDir, fileName);
                plot.Plot.SaveJpeg(path, 1600, 900);
                MessageBox.Show(this, $"График успешно сохранён в:\n{path}", "Сохранено", MessageBoxButtons.OK);
            }
            else
            {
                var dialog = new SaveFileDialog
                {
                    Filters = { new FileDialogFilter("JPEG Image", ".jpg") },
                    FileName = fileName
                };
                if (dialog.ShowDialog(this) == DialogResult.Ok)
                {
                    plot.Plot.SaveJpeg(dialog.FileName, 1600, 900);
                    MessageBox.Show(this, $"График успешно сохранён:\n{dialog.FileName}", "Сохранено", MessageBoxButtons.OK);
                }
            }
        }

        // Показ данных в базе
        private List<(string Method, double MeanMs)> LoadBenchmarkData()
        {
            using var db = new ParallelAnalyzer.Core.Data.BenchmarkDbContext();

            var data = db.NumberBenchmarks
                .OrderBy(r => r.MeanMs)
                .Select(r => new { r.MethodName, r.MeanMs })
                .ToList();

            if (!data.Any())
            {
                MessageBox.Show(this, "В базе нет данных для отображения.", "Информация", MessageBoxButtons.OK);
                lblResult.Text = "Нет данных для отображения.";
                return new();
            }

            lblResult.Text = $"Загружено {data.Count} результатов из базы.";
            return data.Select(x => (x.MethodName, x.MeanMs)).ToList();
        }
        
        // Построение графика из базы данных
        private void ShowDbPlot()
        {
            lblResult.Text = "Загрузка данных из базы...";
            plot.Plot.Clear();

            var data = LoadBenchmarkData();
            if (data.Count == 0)
            {
                lblResult.Text = "В базе нет данных для отображения.";
                return;
            }

            double[] values = data.Select(d => d.MeanMs).ToArray();
            string[] labels = data.Select(d => d.Method).ToArray();

            plot.Plot.Clear();

            var bars = plot.Plot.Add.Bars(values);
            plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);

            // Настройки внешнего вида
            plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 75;
            plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = ScottPlot.Alignment.LowerLeft;
           // plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = ScottPlot.Alignment.UpperRight;
                       
            plot.Plot.Axes.Bottom.TickLabelStyle.OffsetY = -25;
            plot.Plot.Axes.Bottom.TickLabelStyle.OffsetX = -5;
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 9;
            
            plot.Plot.Axes.Margins(bottom: 1.5);
            
                       
            plot.Plot.Axes.AutoScale();
            plot.Plot.RenderInMemory();
            plot.Refresh();

            // Подписи и заголовок
            plot.Plot.Title("Результаты Benchmark из базы данных");
            plot.Plot.YLabel("Время выполнения (мс)");

            lblResult.Text = $"Загружено {data.Count} результатов из базы данных.";
        }

        // Кнопка для сохранения результатов теста в базу
        private void SaveResultsToDatabase()
        {
            string selectedTaskName = cbTask.SelectedValue?.ToString() ?? "Неизвестная задача";
            if (lastResults.Count == 0)
            {
                MessageBox.Show(this, "Нет данных для сохранения. Сначала выполните Benchmark.",
                                "Информация", MessageBoxButtons.OK);
                return;
            }

            var confirm = MessageBox.Show(this,
                $"Будут сохранены результаты {lastResults.Count} методов.\n\n" +
                "Вы уверены, что хотите сохранить данные в базу?",
                "Подтверждение сохранения",
                MessageBoxButtons.YesNo,
                MessageBoxType.Question);

            if (confirm == DialogResult.No)
            {
                lblResult.Text = "Сохранение отменено пользователем.";
                return;
            }

            try
            {
                using var db = new ParallelAnalyzer.Core.Data.BenchmarkDbContext();

                // Проверяем наличие SystemInfo
                var system = db.Systems.FirstOrDefault();
                if (system == null)
                {
                    system = ParallelAnalyzer.Core.Helpers.SystemInfoCollector.GetCurrent();
                    db.Systems.Add(system);
                    db.SaveChanges();
                }

                // Проверяем наличие Task
                bool isNumericTask = (cbCategory.SelectedValue?.ToString() ?? "").Contains("числов");


                var task = db.Tasks.FirstOrDefault(t => t.TaskName == selectedTaskName);
                int selectedArraySize = TaskConfig.ArraySize;

                // если задача уже есть — обновляем InputSize
                if (task != null)
                {
                    if (isNumericTask)
                    {
                        task.InputSize = selectedArraySize;
                        db.SaveChanges();
                    }
                }
                else
                {
                    task = new ParallelAnalyzer.Core.Models.BenchmarkTask
                    {
                        TaskName = selectedTaskName,
                        Description = $"Анализ задачи: {selectedTaskName}",
                        InputSize = isNumericTask ? selectedArraySize : 0
                    };
                    db.Tasks.Add(task);
                    db.SaveChanges();
                }

                /*
                var task = db.Tasks.FirstOrDefault(t => t.TaskName == selectedTaskName);
                if (task == null)
                {
                    int selectedArraySize = 1_000_000;

                    if (isNumericTask)
                    {
                        // Читаем выбранный пользователем размер массива из ComboBox
                        if (cbArraySize.SelectedValue != null &&
                            int.TryParse(cbArraySize.SelectedValue.ToString().Replace(" ", ""), out int parsed))
                            selectedArraySize = parsed;
                        else
                            selectedArraySize = TaskConfig.ArraySize;
                    }

                    task = new ParallelAnalyzer.Core.Models.BenchmarkTask
                    {
                        TaskName = selectedTaskName,
                        Description = $"Анализ задачи: {selectedTaskName}",
                        InputSize = isNumericTask ? selectedArraySize : 0
                    };
                    //task = new ParallelAnalyzer.Core.Models.BenchmarkTask
                    //{
                    //    TaskName = selectedTaskName,
                    //    Description = $"Анализ задачи: {selectedTaskName}",
                    //    InputSize = isNumericTask ? TaskConfig.ArraySize : 0
                    //};
                    db.Tasks.Add(task);
                    db.SaveChanges();
                }
                */

                // Создаём новую сессию
                var session = new ParallelAnalyzer.Core.Models.BenchmarkSession
                {
                    SystemInfoId = system.Id,
                    TaskId = task.Id,
                    Description = $"Запуск {DateTime.Now:dd.MM.yyyy HH:mm:ss} на {system.MachineName}"
                };

                db.Sessions.Add(session);
                db.SaveChanges();

                // Сохраняем результаты с реальным N
                foreach (var r in lastResults)
                {
                    db.NumberBenchmarks.Add(new ParallelAnalyzer.Core.Models.NumberBenchmarkResult
                    {
                        TaskId = task.Id,
                        SystemInfoId = system.Id,
                        SessionId = session.Id,
                        MethodName = r.Method,
                        MeanMs = r.MeanMs,
                        StdDevMs = 0,
                        N = TaskConfig.ArraySize,
                        //N = r.N,
                        Comment = $"- (N={r.N:N0})"
                    });
                }

                db.SaveChanges();
                MessageBox.Show(this, "Результаты успешно сохранены в базу данных.",
                                "Успех", MessageBoxButtons.OK);
                lblResult.Text = "Результаты сохранены.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Ошибка при сохранении:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxType.Error);
                lblResult.Text = "Ошибка при сохранении.";
            }
        }

    }
}

