using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using arsX.Sol_Explorer.Models;

namespace arsX.Sol_Explorer.Services
{
    public static class SiteProfileStore
    {
        private static readonly string ConfigDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

        private static readonly string ConfigPath =
            Path.Combine(ConfigDirectory, "sites.json");

        private static List<SiteProfile>? _sites;

        public static List<SiteProfile> GetSites()
        {
            if (_sites == null)
            {
                LoadSites();
            }

            return _sites!;
        }

        public static void LoadSites()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _sites = new List<SiteProfile>();
                    return;
                }

                var json = File.ReadAllText(ConfigPath);
                _sites = JsonSerializer.Deserialize<List<SiteProfile>>(json)
                         ?? new List<SiteProfile>();
            }
            catch
            {
                // Fail safe with an empty list if anything goes wrong.
                _sites = new List<SiteProfile>();
            }
        }

        public static void UpdateUsername(string siteName, string newUsername)
        {
            if (_sites == null)
            {
                LoadSites();
            }

            var site = _sites?.Find(s => s.Name == siteName);
            if (site != null)
            {
                site.Username = newUsername;
            }
        }

        public static void SaveSites()
        {
            if (_sites == null)
            {
                return;
            }

            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var json = JsonSerializer.Serialize(
                    _sites,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Silently ignore write errors for now.
                // (Later we can add logging or user notification.)
            }
        }
    }
}
