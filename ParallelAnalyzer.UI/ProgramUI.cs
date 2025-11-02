using Eto.Forms;
using Eto.Wpf;
//using static System.Net.Mime.MediaTypeNames;

namespace ParallelAnalyzer.UI
{
    internal static class ProgramUI
    {
        [STAThread]
        static void Main(string[] args)
        {
            new Application(new Eto.Wpf.Platform()).Run(new WelcomeForm());
        }
    }
}