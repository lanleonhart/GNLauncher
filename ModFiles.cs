using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Windows;

namespace GNLauncher
{
    internal class ModFiles
    {   
        string[] URLS = new string[]
        {
            "https://github.com/wmtorode/mechaffinity/releases/download/v1.4.1/MechAffinity.zip",
            "https://github.com/wmtorode/DropCostsEnhanced/releases/download/1.0.0/DropCostsEnhanced.zip",
            "https://github.com/wmtorode/ColourfulFlashPoints/releases/download/1.1.0/ColourfulFlashpoints.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-StarLeague/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-Customs/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-CivilWar/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Clan-GoldenCentury/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-ClanInvasion/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-DarkAge/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Clan-Modern/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-CustomUnits/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Data/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-IS-Mech/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Tools/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Clan-Mech/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Miscellaneous/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Tanks/archive/refs/heads/master.zip",
            "https://github.com/BattletechModders/Community-Asset-Bundle-Maps/archive/refs/heads/master.zip",
            "https://github.com/lanleonhart/Battletech-GN-Wars/archive/refs/heads/development.zip"
        };

        List<string> _downloadedFiles = new List<string>();
        System.Action<int, int, string, HttpProgressEventArgs> _progressCallback = null;
        string _currentURL;

        public async Task Download(System.Action<int, int, string, HttpProgressEventArgs> progressCallback)
        {            
            App.Log.Information("Starting file download");
            _downloadedFiles.Clear();
            _progressCallback = progressCallback;

            var tempPath = Path.Join(App.TempPath, "files");
            try
            {
                if(Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
                
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex) 
            {
                App.Log.Error(ex.ToString());
                MessageBox.Show($"Error occured. See log at {App.TempPath}");
            }

            var handler = new HttpClientHandler() { AllowAutoRedirect = true };
            var progresshandler = new ProgressMessageHandler(handler);
            progresshandler.HttpReceiveProgress += Progresshandlder_HttpReceiveProgress;            
            using (var client = new HttpClient(progresshandler))
            {                
                foreach (string url in URLS)
                {
                    try
                    {
                        App.Log.Information($"Downloading {url}");
                        _currentURL = url;
                        var stream = await client.GetStreamAsync(url);
                        var zipPath = Path.Join(tempPath, MakeFilenameFromGithubURL(url) + ".zip");
                        try
                        {
                            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await stream.CopyToAsync(fileStream);
                                _downloadedFiles.Add(zipPath);                                
                                App.Log.Information($"Downloaded {zipPath}");
                            }
                        }
                        catch (Exception ex) { App.Log.Error(ex.ToString()); }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        MessageBox.Show($"Error occured. See log at {App.TempPath}");
                    }
                }
            }
            App.Log.Information("Finished file download");
        }

        private void Progresshandlder_HttpReceiveProgress(object? sender, HttpProgressEventArgs e)
        {
            _progressCallback?.Invoke(URLS.Length, _downloadedFiles.Count, _currentURL, e);
        }

        string MakeFilenameFromGithubURL(string url)
        {
            var split = url.Split('/');
            return split[4];
        }
    }

}
