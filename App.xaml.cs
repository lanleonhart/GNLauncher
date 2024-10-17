using Serilog.Core;
using Serilog;
using System.Windows;
using System.IO;

namespace GNLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string TempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gnwars_launcher");
        static string LogFile => System.IO.Path.Combine(TempPath, "gnlauncher-log-.txt");
        public static Logger Log;

        public App()
        {           
            try { Directory.CreateDirectory(TempPath); }catch(System.Exception ex) { MessageBox.Show(ex.ToString()); }
            Log = new LoggerConfiguration().WriteTo.File(LogFile, rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("----------------------------------------------------------------");
            Log.Information(DateTime.Now.ToString());
        }
    }
}