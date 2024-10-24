using Microsoft.Win32;
using System.IO;
using System.Net.Http.Handlers;
using System.Windows;


namespace GNLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            button_start_install.IsEnabled = false;
        }

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select BattleTech.exe";
            openFile.Filter = "*.exe | BattleTech.exe";
            openFile.ShowDialog();
            if (File.Exists(openFile.FileName))
            {
                textBox_exe_loc.Text = openFile.FileName; 
                App.Log.Information($"BattleTech.exe location: {openFile.FileName}");
                button_start_install.IsEnabled = true;
            }
            else
                button_start_install.IsEnabled = false;
        }

        async private void button_start_install_Click(object sender, RoutedEventArgs e)
        {
            App.Log.Information("Starting mod installation");
            step_select_exe.Visibility = Visibility.Hidden;
            step_downloading.Visibility = Visibility.Visible;

            var files = new ModFiles();
            files.OnErrorOccured += Files_OnErrorOccured;

            //download files
            await files.Download(UpdateDownloadProgress);

            step_downloading.Visibility = Visibility.Hidden;
            step_installing.Visibility = Visibility.Visible;

            //extract files
            await files.ExtractFiles(textBox_exe_loc.Text, (msg, total, count) => 
            {
                step_installing.Dispatcher.Invoke(() =>
                {
                    lblInstall.Content = msg;
                    installProgressTotal.Maximum = total;
                    installProgressTotal.Value = count;
                });
            });

            //copy files
            await files.CopyFiles(textBox_exe_loc.Text, (msg, total, count) =>
            {
                step_installing.Dispatcher.Invoke(() =>
                {
                    lblInstall.Content = msg;
                    installProgressTotal.Maximum = total;
                    installProgressTotal.Value = count;
                });
            });

            App.Log.Information("Finished mod installation");
        }

        private void Files_OnErrorOccured()
        {
            MessageBox.Show("An error occured during install. Contact Support with log files");
            System.Diagnostics.Process.Start("explorer", App.TempPath);
            Application.Current.Shutdown();
        }

        private void button_open_log_Click(object sender, RoutedEventArgs e)
        {            
            System.Diagnostics.Process.Start("explorer", App.TempPath);
        }

        void UpdateDownloadProgress(int totalFiles, int filesDownloaded, string currentFile, HttpProgressEventArgs args)
        {
            step_downloading.Dispatcher.BeginInvoke(() =>
            {
                progressTotal.Maximum = totalFiles;
                progressTotal.Value = filesDownloaded;

                label_current_file.Content = $"Downloading {currentFile}";

                if (args != null)
                {
                    if (args.TotalBytes.HasValue)
                    {
                        progressCurrent.IsIndeterminate = false;
                        progressCurrent.Maximum = args.TotalBytes.Value;
                        progressCurrent.Value = args.BytesTransferred;
                        tb_progress.Text = $"{args.ProgressPercentage}%";
                    }
                    else
                    {
                        progressCurrent.IsIndeterminate = true;
                        tb_progress.Text = $"{args.BytesTransferred}";
                    }
                }
            });
        }
    }
}