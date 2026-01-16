using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using arsX.Sol_Explorer.Models;

namespace arsX.Sol_Explorer.Services
{
    public class CredentialMetadataStore
    {
        private readonly string _filePath;
        private readonly List<SiteCredential> _credentials = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public CredentialMetadataStore()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArsX", "OneView");
            Directory.CreateDirectory(appData);
            _filePath = Path.Combine(appData, "sites.json");
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var creds = JsonSerializer.Deserialize<List<SiteCredential>>(json, _jsonOptions);
                    if (creds != null)
                        _credentials = creds;
                }
                catch { }
            }
        }

        public IReadOnlyList<SiteCredential> GetAll() => _credentials.AsReadOnly();

        public SiteCredential? GetById(Guid id) => _credentials.FirstOrDefault(c => c.Id == id);

        public IEnumerable<SiteCredential> GetByDomain(string domain)
        {
            var norm = NormalizeDomain(domain);
            return _credentials.Where(c => NormalizeDomain(c.Domain) == norm);
        }

        public void AddOrUpdate(SiteCredential credential)
        {
            if (credential.Id == Guid.Empty)
                credential.Id = Guid.NewGuid();
            var idx = _credentials.FindIndex(c => c.Id == credential.Id);
            if (idx >= 0)
                _credentials[idx] = credential;
            else
                _credentials.Add(credential);
            Save();
        }

        public void Remove(Guid id)
        {
            _credentials.RemoveAll(c => c.Id == id);
            Save();
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_credentials, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }

        private static string NormalizeDomain(string domain)
        {
            if (domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                return domain.Substring(4).ToLowerInvariant();
            return domain.ToLowerInvariant();
        }
    }
}
