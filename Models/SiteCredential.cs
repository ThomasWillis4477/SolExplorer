using System;

namespace arsX.Sol_Explorer.Models
{
    public class SiteCredential
    {
        public Guid Id { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
