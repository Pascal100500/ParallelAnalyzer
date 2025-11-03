using Eto.Forms;
using Eto.Drawing;
using ParallelAnalyzer.Core.Data;
using ParallelAnalyzer.Core.Models;
using ScottPlot.Eto;
using System.Linq;

namespace ParallelAnalyzer.UI
{
    public class ResultsWindow : Form
    {
        private readonly GridView<SessionViewModel> gridSessions;
        private readonly GridView<ResultViewModel> gridResults;
        private readonly EtoPlot plot;

        private readonly DropDown cbCpuFilter = new() { Width = 250 };
        private readonly DropDown cbTaskFilter = new() { Width = 250 };
        private readonly Button btnResetFilters = new() { Text = "Сбросить фильтры" };
        private readonly Button btnRefresh = new() { Text = "Обновить список" };
        private readonly Button btnDeleteSession = new() { Text = "Удалить сессию", Enabled = false };

        private List<SessionViewModel> allSessions = new();

        public ResultsWindow()
        {
            Title = "Результаты тестов";
            ClientSize = new Size(1200, 1000);

            plot = new EtoPlot { Size = new Size(900, 400) };

            // Таблица с сессиями
            gridSessions = new GridView<SessionViewModel>
            {
                Height = 250,
                AllowMultipleSelection = false
            };
            gridSessions.Columns.Add(new GridColumn { HeaderText = "ID", DataCell = new TextBoxCell(nameof(SessionViewModel.Id)), Width = 50 });
            gridSessions.Columns.Add(new GridColumn { HeaderText = "Дата", DataCell = new TextBoxCell(nameof(SessionViewModel.Date)), Width = 150 });
            gridSessions.Columns.Add(new GridColumn { HeaderText = "Процессор", DataCell = new TextBoxCell(nameof(SessionViewModel.CpuModel)), Width = 250 });
            gridSessions.Columns.Add(new GridColumn { HeaderText = "Ядер", DataCell = new TextBoxCell(nameof(SessionViewModel.CpuCores)), Width = 60 });
            gridSessions.Columns.Add(new GridColumn { HeaderText = "Задача", DataCell = new TextBoxCell(nameof(SessionViewModel.TaskName)), Width = 220 });
            gridSessions.Columns.Add(new GridColumn { HeaderText = "Размер N", DataCell = new TextBoxCell(nameof(SessionViewModel.N)), Width = 100 });

            // Таблица с результатами выбранной сессии
            gridResults = new GridView<ResultViewModel>
            {
                Height = 200,
                AllowMultipleSelection = false
            };
            gridResults.Columns.Add(new GridColumn { HeaderText = "Метод", DataCell = new TextBoxCell(nameof(ResultViewModel.MethodName)), Width = 300 });
            gridResults.Columns.Add(new GridColumn { HeaderText = "Среднее (мс)", DataCell = new TextBoxCell(nameof(ResultViewModel.MeanMs)), Width = 120 });
            gridResults.Columns.Add(new GridColumn { HeaderText = "Комментарий", DataCell = new TextBoxCell(nameof(ResultViewModel.Comment)), Width = 250 });

            // Кнопка удаления
            btnDeleteSession.Click += (s, e) =>
            {
                if (gridSessions.SelectedItem is not SessionViewModel selected)
                {
                    MessageBox.Show(this, "Сначала выберите сессию для удаления.", "Информация", MessageBoxButtons.OK);
                    return;
                }

                var confirm = MessageBox.Show(this,
                    $"Вы действительно хотите удалить сессию №{selected.Id} ({selected.TaskName})?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxType.Warning);

                if (confirm == DialogResult.No)
                    return;

                try
                {
                    using var db = new BenchmarkDbContext();

                    // Удаляем все результаты, связанные с этой сессией
                    var resultsToDelete = db.NumberBenchmarks.Where(r => r.SessionId == selected.Id).ToList();
                    db.NumberBenchmarks.RemoveRange(resultsToDelete);

                    // Удаляем саму сессию
                    var session = db.Sessions.FirstOrDefault(s => s.Id == selected.Id);
                    if (session != null)
                        db.Sessions.Remove(session);

                    db.SaveChanges();

                    MessageBox.Show(this, $"Сессия №{selected.Id} удалена.", "Успех", MessageBoxButtons.OK);

                    // обновляем список
                    LoadSessions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Ошибка при удалении:\n{ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            };

            // Панель фильтров и кнопок
            var filtersPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Items =
                {
                    new Label { Text = "Фильтр по процессору:" },
                    cbCpuFilter,
                    new Label { Text = "Фильтр по задаче:" },
                    cbTaskFilter,
                    btnRefresh,
                    btnResetFilters,
                    btnDeleteSession 
                }
            };

            // Основное содержимое окна
            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    filtersPanel,
                    new Label { Text = "Список сессий:" },
                    gridSessions,
                    new Label { Text = "Результаты выбранной сессии:" },
                    gridResults,
                    plot
                }
            };

            // Обработчики событий
            gridSessions.SelectionChanged += (s, e) =>
            {
                if (gridSessions.SelectedItem is SessionViewModel selected)
                {
                    btnDeleteSession.Enabled = true; 
                    ShowSessionResults(selected);
                }
                else
                {
                    btnDeleteSession.Enabled = false;
                }
            };

            cbCpuFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            cbTaskFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            btnResetFilters.Click += (s, e) => ResetFilters();
            btnRefresh.Click += (s, e) => LoadSessions();

            LoadSessions();
        }

        // ======================
        // Методы загрузки данных
        // ======================
        private void LoadSessions()
        {
            using var db = new BenchmarkDbContext();

            var query = from s in db.Sessions
                        join sys in db.Systems on s.SystemInfoId equals sys.Id
                        join task in db.Tasks on s.TaskId equals task.Id
                        orderby s.Id ascending
                        select new SessionViewModel
                        {
                            Id = s.Id,
                            Date = s.Description,
                            CpuModel = sys.ProcessorName,
                            CpuCores = sys.LogicalCores,
                            TaskName = task.TaskName,
                            N = task.InputSize
                        };

            allSessions = query.ToList();
            gridSessions.DataStore = allSessions;
            FillFilters();
        }

        private void FillFilters()
        {
            cbCpuFilter.Items.Clear();
            cbTaskFilter.Items.Clear();

            cbCpuFilter.Items.Add("Все процессоры");
            cbTaskFilter.Items.Add("Все задачи");

            foreach (var cpu in allSessions.Select(s => s.CpuModel).Distinct().OrderBy(x => x))
                cbCpuFilter.Items.Add(cpu);

            foreach (var task in allSessions.Select(s => s.TaskName).Distinct().OrderBy(x => x))
                cbTaskFilter.Items.Add(task);

            cbCpuFilter.SelectedIndex = 0;
            cbTaskFilter.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            string selectedCpu = cbCpuFilter.SelectedValue?.ToString() ?? "Все процессоры";
            string selectedTask = cbTaskFilter.SelectedValue?.ToString() ?? "Все задачи";

            var filtered = allSessions.AsEnumerable();

            if (selectedCpu != "Все процессоры")
                filtered = filtered.Where(s => s.CpuModel == selectedCpu);

            if (selectedTask != "Все задачи")
                filtered = filtered.Where(s => s.TaskName == selectedTask);

            gridSessions.DataStore = filtered.ToList();
        }

        private void ResetFilters()
        {
            cbCpuFilter.SelectedIndex = 0;
            cbTaskFilter.SelectedIndex = 0;
            gridSessions.DataStore = allSessions;
        }

        private void ShowSessionResults(SessionViewModel session)
        {
            using var db = new BenchmarkDbContext();

            var results = db.NumberBenchmarks
                .Where(r => r.SessionId == session.Id)
                .OrderBy(r => r.MeanMs)
                .Select(r => new ResultViewModel
                {
                    MethodName = r.MethodName,
                    MeanMs = r.MeanMs.ToString("0.000"),
                    Comment = r.Comment ?? ""
                })
                .ToList();

            gridResults.DataStore = results;

            plot.Plot.Clear();

            if (results.Count > 0)
            {
                double[] means = results.Select(r => double.Parse(r.MeanMs)).ToArray();
                string[] labels = results.Select(r => r.MethodName).ToArray();

                plot.Plot.Add.Bars(means);
                plot.Plot.Axes.Bottom.TickGenerator =
                    new ScottPlot.TickGenerators.NumericManual(
                        Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);

                //plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 75;
                //plot.Plot.Axes.Margins(bottom: 0.4);
                //plot.Plot.Title($"Результаты сессии #{session.Id}");
                //plot.Plot.YLabel("Время (мс)");
                plot.Plot.Axes.Margins(bottom: 0.7);
                plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 75;
                //plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = ScottPlot.Alignment.LowerLeft;
                plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = ScottPlot.Alignment.UpperRight;
                                
                plot.Plot.Axes.Bottom.TickLabelStyle.OffsetY = -10;

                // Дополнительный отступ снизу для длинных надписей
                plot.Plot.Axes.Margins(bottom: 0.6);
                plot.Plot.Axes.AutoScale();
                plot.Plot.RenderInMemory(); 
                plot.Refresh();
            }
        }

        // ============================
        // Внутренние классы представлений
        // ============================
        private class SessionViewModel
        {
            public int Id { get; set; }
            public string Date { get; set; } = "";
            public string CpuModel { get; set; } = "";
            public int CpuCores { get; set; }
            public string TaskName { get; set; } = "";
            public int N { get; set; }
        }

        private class ResultViewModel
        {
            public string MethodName { get; set; } = "";
            public string MeanMs { get; set; } = "";
            public string Comment { get; set; } = "";
        }
    }
}

