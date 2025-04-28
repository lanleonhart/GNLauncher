// FULL UPDATED ModFiles.cs
// Handles GN-Wars first, then DLL-only copy for MechAffinity, DropCostsEnhanced, ColourfulFlashpoints

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

        string[] ALL_URLS = new string[]
        {
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
            "https://github.com/lanleonhart/Battletech-GN-Wars/archive/refs/tags/0.1.0.zip"
        };

        string[] DLL_ONLY_URLS = new string[]
        {
            "https://github.com/wmtorode/mechaffinity/releases/download/v1.4.1/MechAffinity.zip",
            "https://github.com/wmtorode/DropCostsEnhanced/releases/download/1.0.0/DropCostsEnhanced.zip",
            "https://github.com/wmtorode/ColourfulFlashPoints/releases/download/1.1.0/ColourfulFlashpoints.zip"
        };

        List<string> _downloadedFiles = new List<string>();
        List<string> _extractedFiles = new List<string>();
        System.Action<int, int, string, HttpProgressEventArgs> _progressCallback = null!;
        string _currentURL = "";

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
                    if (MessageBox.Show("Temp directory exists. Clear and re-download?", "GNWars Mod", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        Directory.Delete(tempPath, true);
                }
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                App.Log.Error(ex.ToString());
                MessageBox.Show($"Error occurred. See log at {App.TempPath}");
            }

            await DownloadFiles(ALL_URLS, tempPath);
        }

        private async Task DownloadFiles(string[] urls, string tempPath)
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = true };
            var progresshandler = new ProgressMessageHandler(handler);
            progresshandler.HttpReceiveProgress += Progresshandlder_HttpReceiveProgress;
            using (var client = new HttpClient(progresshandler))
            {
                foreach (string url in urls)
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
                            _progressCallback?.Invoke(urls.Length, _downloadedFiles.Count, _currentURL, null);
                            continue;
                        }

                        var stream = await client.GetStreamAsync(url);
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

            App.Log.Information("Finished file download");
        }

        public async Task ExtractAndInstall(string exeFolder)
        {
            var tempPath = Path.Join(App.TempPath, "extracted");
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

            foreach (var zip in _downloadedFiles)
            {
                var name = Path.GetFileNameWithoutExtension(zip).ToLower();

                if (name.Contains("mechaffinity") || name.Contains("dropcostsenhanced") || name.Contains("colourfulflashpoints"))
                    continue;

                try
                {
                    ZipFile.ExtractToDirectory(zip, tempPath, true);
                }
                catch (Exception ex)
                {
                    App.Log.Error(ex.ToString());
                    OnErrorOccured?.Invoke();
                }
            }

            await InstallExtractedMods(tempPath, exeFolder);

            foreach (var zip in _downloadedFiles)
            {
                var name = Path.GetFileNameWithoutExtension(zip).ToLower();
                if (name.Contains("mechaffinity") || name.Contains("dropcostsenhanced") || name.Contains("colourfulflashpoints"))
                {
                    ExtractDllOnly(zip, exeFolder);
                }
            }
        }

        private void ExtractDllOnly(string zipPath, string exeFolder)
        {
            try
            {
                using (var zip = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = entry.FullName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                var modName = parts[0];
                                var destFolder = Path.Combine(Path.GetDirectoryName(exeFolder)!, "Mods", modName);
                                var destFile = Path.Combine(destFolder, Path.GetFileName(entry.FullName));

                                Directory.CreateDirectory(destFolder);
                                entry.ExtractToFile(destFile, true);

                                App.Log.Information($"Extracted DLL {destFile}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log.Error(ex.ToString());
                OnErrorOccured?.Invoke();
            }
        }

        private async Task InstallExtractedMods(string sourceRoot, string exeFolder)
        {
            var modsFolder = Path.Combine(Path.GetDirectoryName(exeFolder)!, "Mods");

            var topLevelDirs = Directory.GetDirectories(sourceRoot);
            if (topLevelDirs.Length == 1)
            {
                var innerRoot = topLevelDirs[0];
                topLevelDirs = Directory.GetDirectories(innerRoot);

                foreach (var folder in topLevelDirs)
                {
                    var modName = Path.GetFileName(folder);
                    var destPath = Path.Combine(modsFolder, modName);
                    if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
                    DirectoryCopy(folder, destPath, true);
                    App.Log.Information($"Installed {modName}");
                }
            }
            else
            {
                foreach (var folder in topLevelDirs)
                {
                    var modName = Path.GetFileName(folder);
                    var destPath = Path.Combine(modsFolder, modName);
                    if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
                    DirectoryCopy(folder, destPath, true);
                    App.Log.Information($"Installed {modName}");
                }
            }
        }

        private void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, true);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDir, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private void Progresshandlder_HttpReceiveProgress(object? sender, HttpProgressEventArgs e)
        {
            _progressCallback?.Invoke(ALL_URLS.Length, _downloadedFiles.Count, _currentURL, e);
        }

        string MakeFilenameFromGithubURL(string url)
        {
            var split = url.Split('/');
            return split[split.Length - 1].Replace(".zip", "");
        }
    }
}
