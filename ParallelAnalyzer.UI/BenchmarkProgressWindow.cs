using Eto.Forms;
using Eto.Drawing;

namespace ParallelAnalyzer.UI
{
    public class BenchmarkProgressWindow : Form
    {
        private readonly Label lblStatus;
        private readonly ProgressBar progress;

        public BenchmarkProgressWindow()
        {
            Title = "Выполнение Benchmark";
            ClientSize = new Size(400, 150);
            Resizable = false;
            Topmost = true;

            lblStatus = new Label
            {
                Text = "Запуск тестов BenchmarkDotNet...",
                TextAlignment = TextAlignment.Center
            };

            progress = new ProgressBar
            {
                Indeterminate = true,
                Width = 350
            };

            Content = new StackLayout
            {
                Padding = 15,
                Spacing = 10,
                Items =
                {
                    new Label
                    {
                        Text = "Пожалуйста, подождите...",
                        Font = new Font(FontFamilies.Sans, 12, FontStyle.Bold),
                        TextAlignment = TextAlignment.Center
                    },
                    lblStatus,
                    progress
                }
            };
        }

        public void UpdateStatus(string message)
        {
            Application.Instance.Invoke(() => lblStatus.Text = message);
        }
    }
}
