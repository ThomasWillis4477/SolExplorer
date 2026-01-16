using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace arsX.Sol_Explorer.Services
{
    public class DpapiCredentialStore : ICredentialStore
    {
        private static readonly string VaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArsX", "OneView", "vault.bin");

        private Dictionary<string, Dictionary<string, string>> _vault = new();

        public DpapiCredentialStore()
        {
            LoadVault();
        }

        public async Task SetPasswordAsync(string siteKey, string username, string password)
        {
            if (!_vault.ContainsKey(siteKey))
                _vault[siteKey] = new Dictionary<string, string>();
            _vault[siteKey][username] = password;
            await SaveVaultAsync();
        }

        public Task<string?> GetPasswordAsync(string siteKey, string username)
        {
            if (_vault.TryGetValue(siteKey, out var users) && users.TryGetValue(username, out var pwd))
                return Task.FromResult<string?>(pwd);
            return Task.FromResult<string?>(null);
        }

        public async Task RemovePasswordAsync(string siteKey, string username)
        {
            if (_vault.TryGetValue(siteKey, out var users))
            {
                users.Remove(username);
                if (users.Count == 0)
                    _vault.Remove(siteKey);
                await SaveVaultAsync();
            }
        }

        private void LoadVault()
        {
            if (!File.Exists(VaultPath))
            {
                _vault = new();
                return;
            }
            var encrypted = File.ReadAllBytes(VaultPath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            _vault = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json) ?? new();
        }

        private async Task SaveVaultAsync()
        {
            var json = JsonSerializer.Serialize(_vault);
            var plain = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            var dir = Path.GetDirectoryName(VaultPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            await File.WriteAllBytesAsync(VaultPath, encrypted);
        }
    }
}
