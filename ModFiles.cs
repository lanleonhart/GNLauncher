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
            "https://github.com/lanleonhart/CAB-3025/archive/refs/tags/DL.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-IS-ClanInvasion/archive/refs/tags/release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-IS-CivilWar/archive/refs/tags/Release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-IS-Customs/archive/refs/tags/Release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-IS-StarLeague/archive/refs/tags/Release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-Clan-GoldenCentury/archive/refs/tags/release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-Clan-Modern/archive/refs/tags/Release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-CustomUnits/archive/refs/tags/Release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-Tanks/archive/refs/tags/release.zip",
            "https://github.com/lanleonhart/Community-Asset-Bundle-Miscellaneous/archive/refs/tags/release.zip",
            "https://github.com/lanleonhart/Battletech-GN-Wars/archive/refs/tags/Release.zip"
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
            _progressCallback.Invoke(1, 0, "Downloading files...", null);

            var tempPath = Path.Join(App.TempPath, "files");
            try
            {
                if (Directory.Exists(tempPath))
                {
                    if (MessageBox.Show("Temp directory exists clear and re-download all mod files?", "GNWars Mod", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Directory.Delete(tempPath, true);
                }

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
            progressCallback.Invoke("Extracting files...", 1, 0);
            var tempPath = Path.Join(App.TempPath, "extracted");
            await Task.Run(() =>
            {
                foreach (var file in _downloadedFiles)
                {
                    try
                    {
                        using (var zip = ZipFile.OpenRead(file))
                        {
                            var extractedPath = Path.Join(tempPath, zip.Entries[0].FullName);
                            if (!Directory.Exists(extractedPath))
                                zip.ExtractToDirectory(tempPath, true);

                            _extractedFiles.Add(extractedPath);
                            progressCallback.Invoke("Extracting Files...", _downloadedFiles.Count, _extractedFiles.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        OnErrorOccured?.Invoke();
                        return;
                    }
                }
            });
        }

        public async Task CopyFiles(string exeFolder, Action<string, int, int> progressCallback)
        {
            progressCallback.Invoke("Copying files...", 0, 1);
            var modFolder = Path.Join(Path.GetDirectoryName(exeFolder), "Mods");
            int count = 0;
            int total = _downloadedFiles.Count;
            foreach (var folder in _extractedFiles)
            {
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
                        await InstallMod(folder, installFolder, modName);
                        progressCallback.Invoke("Copying Files", total, ++count);
                    }
                }
                else if (folder.Contains("GN-Wars"))
                {
                    var gnFolders = Directory.GetDirectories(folder);
                    total += gnFolders.Length;
                    progressCallback.Invoke("Copying files...", total, ++count);
                    foreach (var f in gnFolders)
                    {
                        if (f.Contains("ModSaves"))
                            continue;

                        split = f.Split(new char[] { Path.DirectorySeparatorChar, '/' });
                        modName = split[split.Length - 1];
                        installFolder = Path.Combine(modFolder, modName);
                        await InstallMod(f, installFolder, modName, false);
                        progressCallback.Invoke("Copying Files", total, ++count);
                    }
                }
                else
                {
                    split = folder.Split(Path.DirectorySeparatorChar);
                    modName = split[split.Length - 1].Replace("/", "");
                    installFolder = Path.Combine(modFolder, modName);
                    await InstallMod(folder, installFolder, modName);
                    progressCallback.Invoke("Copying Files", total, ++count);
                }
            }
        }

        async Task InstallMod(string sourceFolder, string installFolder, string modName, bool askForOverwrite = false)
        {
            App.Log.Information($"Installing {modName} to {installFolder}");
            if (Directory.Exists(installFolder))
            {
                if (askForOverwrite && MessageBox.Show($"Mod {modName} already exists. Overwrite?", $"Mod {modName} exists", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    try { Directory.Delete(installFolder, true); }
                    catch (Exception ex)
                    {
                        App.Log.Error(ex.ToString());
                        OnErrorOccured?.Invoke();
                    }
                }
            }

            await Task.Run(() =>
            {
                if (sourceFolder.Contains("Community-Asset-Bundle"))
                {
                    var list = Directory.GetDirectories(sourceFolder);
                    if (list.Length > 0)
                        MoveFolder(list[0], installFolder);

                    try { Directory.Delete(sourceFolder, true); } catch (Exception ex) { App.Log.Error(ex.ToString()); }
                }
                else
                {
                    MoveFolder(sourceFolder, installFolder);
                }
            });
        }

        void MoveFolder(string source, string destination)
        {
            App.Log.Information($"Moving {source} to {destination}");

            try
            {
                if (Path.GetPathRoot(source) != Path.GetPathRoot(destination))
                {
                    App.Log.Information($"Different drives detected. Performing copy and delete instead of move.");
                    CopyFolder(source, destination);
                    Directory.Delete(source, true);
                    App.Log.Information($"Copied and deleted {source} to {destination}");
                }
                else
                {
                    Directory.Move(source, destination);
                    App.Log.Information($"Moved {source} to {destination}");
                }
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

        void CopyFolder(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, file);
                var destPath = Path.Combine(destination, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                File.Copy(file, destPath, true);
            }
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
