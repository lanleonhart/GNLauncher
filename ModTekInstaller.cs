using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace GNLauncher
{
    internal class ModTekInstaller
    {
        static readonly string ModTekURL = "https://github.com/BattletechModders/ModTek/releases/download/v4.4.10/ModTek.zip";

        public static async Task InstallModTekIfMissing(string battletechRootFolder)
        {
            try
            {
                string doorstopPath = Path.Combine(battletechRootFolder, "doorstop_config.ini");
                string winhttpPath = Path.Combine(battletechRootFolder, "winhttp.dll");

                if (File.Exists(doorstopPath) && File.Exists(winhttpPath))
                {
                    App.Log.Information("ModTek doorstop files already exist. Skipping ModTek install.");
                    return;
                }

                App.Log.Information("ModTek files missing. Downloading and installing ModTek.");

                var tempModTekZip = Path.Combine(App.TempPath, "modtek.zip");
                Directory.CreateDirectory(App.TempPath);

                using (var client = new HttpClient())
                {
                    var stream = await client.GetStreamAsync(ModTekURL);
                    using (var fileStream = new FileStream(tempModTekZip, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                var extractFolder = Path.Combine(App.TempPath, "modtek_extracted");
                if (Directory.Exists(extractFolder))
                    Directory.Delete(extractFolder, true);

                ZipFile.ExtractToDirectory(tempModTekZip, extractFolder);

                // Copy extracted ModTek files to BattleTech root
                foreach (var file in Directory.GetFiles(extractFolder))
                {
                    var filename = Path.GetFileName(file);
                    var destination = Path.Combine(battletechRootFolder, filename);
                    File.Copy(file, destination, true);
                }

                App.Log.Information("ModTek successfully installed.");
            }
            catch (Exception ex)
            {
                App.Log.Error($"Error installing ModTek: {ex}");
            }
        }
    }
}
