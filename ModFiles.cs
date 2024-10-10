using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;

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


        public async Task Download()
        {
            var tempPath = Path.Join(Path.GetTempPath(), "gnwars");

            try
            {
                Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }

            
            foreach (string url in URLS)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);

                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        var stream = await response.Content.ReadAsStreamAsync();
                        var zipPath = Path.Join(tempPath, Path.GetRandomFileName() + ".zip");
                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    catch(Exception ex) { MessageBox.Show(ex.ToString());  }
                }
            }
        }
    }
}
