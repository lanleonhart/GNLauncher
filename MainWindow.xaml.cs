using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                button_start_install.IsEnabled = true;
            }
            else
                button_start_install.IsEnabled = false;
        }

        async private void button_start_install_Click(object sender, RoutedEventArgs e)
        {            
            step_select_exe.Visibility = Visibility.Hidden;
            var files = new ModFiles();
            await files.Download();
            step_select_exe.Visibility = Visibility.Visible;            
        }
    }
}