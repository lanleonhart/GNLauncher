using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Windows;

namespace GNLauncher
{
    internal class ModFiles
    {
        public delegate void ErrorOccured();
        public event ErrorOccured? OnErrorOccured;

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
        List<string> _extractedFiles = new List<string>();
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
                if (Directory.Exists(tempPath))
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
                        var zipPath = Path.Join(tempPath, MakeFilenameFromGithubURL(url) + ".zip");
                        if (File.Exists(zipPath))
                        {
                            _downloadedFiles.Add(zipPath);
                            App.Log.Information($"Existing file {zipPath}");
                            _progressCallback?.Invoke(URLS.Length, _downloadedFiles.Count, _currentURL, null);
                            continue;
                        }
                        else
                        {
                            var stream = await client.GetStreamAsync(url);
                            try
                            {
                                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await stream.CopyToAsync(fileStream);
                                    _downloadedFiles.Add(zipPath);
                                    App.Log.Information($"Downloaded {zipPath}");
                                }
                            }
                            catch (Exception ex) 
                            { 
                                App.Log.Error(ex.ToString());
                                OnErrorOccured?.Invoke();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        OnErrorOccured?.Invoke();
                        return;                        
                    }
                }
            }
            App.Log.Information("Finished file download");
        }

        public async Task ExtractFiles(string exeFolder, Action<string, int, int> progressCallback)
        {
            var tempPath = Path.Join(App.TempPath, "extracted");
            int count = 0;
            foreach (var file in _downloadedFiles)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using (var zip = ZipFile.OpenRead(file))
                        {
                            var extractedPath = Path.Join(tempPath, zip.Entries[0].FullName);
                            if(!Directory.Exists(extractedPath))
                                zip.ExtractToDirectory(tempPath, true);
                                                       
                            _extractedFiles.Add(extractedPath);
                            progressCallback.Invoke("Extracting Files", _extractedFiles.Count, ++count);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        OnErrorOccured?.Invoke();
                        return;
                    }
                });
            }
        }

        public async Task CopyFiles(string exeFolder, Action<string, int, int> progressCallback)
        {
            var modFolder = Path.Join(Path.GetDirectoryName(exeFolder), "Mods");
            int count = 0;            
            foreach (var folder in _extractedFiles)
            {
                count++;
                string[] split;
                string modName = string.Empty;
                string installFolder = string.Empty;

                if (folder.Contains("Community-Asset-Bundle"))
                {
                    var list = Directory.GetDirectories(folder);
                    if (list.Length > 0)
                    {
                        split = list[0].Split(new char[] { Path.DirectorySeparatorChar, '/' });
                        modName = split[split.Length - 1];
                        installFolder = Path.Combine(modFolder, modName);
                        await InstallMod(folder, installFolder, modName, progressCallback, count);
                    }
                }
                else if (folder.Contains("GN-Wars"))
                {
                    var gnFolders = Directory.GetDirectories(folder);
                    foreach (var f in gnFolders)
                    {
                        if (f.Contains("ModSaves"))
                            continue;

                        split = f.Split(new char[] { Path.DirectorySeparatorChar, '/' });
                        modName = split[split.Length - 1];
                        installFolder = Path.Combine(modFolder, modName);
                        await InstallMod(f, installFolder, modName, progressCallback, count);
                    }
                }
                else
                {
                    split = folder.Split(Path.DirectorySeparatorChar);
                    modName = split[split.Length - 1].Replace("/", "");
                    installFolder = Path.Combine(modFolder, modName);
                    await InstallMod(folder, installFolder, modName, progressCallback, count);
                }                
            }
        }

        async Task InstallMod(string sourceFolder, string installFolder, string modName, Action<string, int, int> progressCallback, int count)
        {
            App.Log.Information($"Installing {modName} to {installFolder}");
            if (Directory.Exists(installFolder))
            {
                //if (MessageBox.Show($"Mod {modName} alreadys exists. Overwrite?", $"Mod {modName} exists", MessageBoxButton.YesNo) == MessageBoxResult.No)
                //{
                //    progressCallback.Invoke("Copying Files", _downloadedFiles.Count, count);
                //    return;
                //}
                //else
                //{
                    try { Directory.Delete(installFolder, true); }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        OnErrorOccured?.Invoke();
                    }
                //}
            }

            await Task.Run(() =>
            {
                if (sourceFolder.Contains("Community-Asset-Bundle"))
                {
                    var list = Directory.GetDirectories(sourceFolder);
                    if (list.Length > 0)
                        MoveFolder(list[0], installFolder);

                    //delete parent folder when done
                    try { Directory.Delete(sourceFolder, true); } catch (Exception ex) { App.Log.Error(ex.ToString()); }
                }                
                else
                {
                    MoveFolder(sourceFolder, installFolder);
                }

                progressCallback.Invoke("Copying Files", _downloadedFiles.Count, count);
            });
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

        /// <summary>
        /// Move the extracted mod folder to the games mod folder
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>      
        void MoveFolder(string source, string destination)
        {
            App.Log.Information($"Moving {source} to {destination}");
            try
            {
                Directory.Move(source, destination);
                App.Log.Information($"Copied {source} to {destination}");
            }
            catch (UnauthorizedAccessException ua)
            {                
                App.Log.Error($"Error moving {source} to {destination}: {ua}");
                OnErrorOccured?.Invoke();
            }
            catch (Exception ex)
            {
                App.Log.Error(ex.ToString());
                OnErrorOccured?.Invoke();
            }
        }
    }

}
