using System.Windows;
using arsX.Sol_Explorer.Models;
using arsX.Sol_Explorer.Services;

namespace arsX.Sol_Explorer
{
    public partial class ManageCredentialsWindow : Window
    {
        private readonly SiteProfile _site;
        private readonly ICredentialStore _credentialStore;

        public ManageCredentialsWindow(SiteProfile site, ICredentialStore credentialStore)
        {
            InitializeComponent();
            _site = site;
            _credentialStore = credentialStore;

            SiteNameTextBlock.Text = _site.Name;
            SiteUrlTextBlock.Text = _site.Url ?? string.Empty;
            UsernameTextBox.Text = _site.Username ?? string.Empty;

            Loaded += async (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(_site.Url) &&
                    !string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    var pw = await _credentialStore.GetPasswordAsync(_site.Url, UsernameTextBox.Text);
                    if (!string.IsNullOrEmpty(pw))
                    {
                        PasswordBox.Text = pw;
                    }
                }
            };

            SaveButton.Click   += SaveButton_Click;
            ClearButton.Click  += ClearButton_Click;
            CancelButton.Click += (_, __) => DialogResult = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update username in SiteProfile and save
            if (UsernameTextBox != null)
                _site.Username = UsernameTextBox.Text;
            var safeUrl = _site.Url ?? string.Empty;
            var safeUsername = _site.Username ?? string.Empty;
            SiteProfileStore.UpdateUsername(safeUrl, safeUsername);
            SiteProfileStore.SaveSites();

            // Store password in vault
            if (!string.IsNullOrWhiteSpace(_site.Url) && !string.IsNullOrWhiteSpace(_site.Username) && PasswordBox != null)
            {
                await _credentialStore.SetPasswordAsync(_site.Url, _site.Username, PasswordBox.Text);
            }
            DialogResult = true;
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear username and save
            _site.Username = string.Empty;
            var safeUrl = _site.Url ?? string.Empty;
            var safeUsername = _site.Username ?? string.Empty;
            SiteProfileStore.UpdateUsername(safeUrl, safeUsername);
            SiteProfileStore.SaveSites();

            // Remove password from vault
            if (!string.IsNullOrWhiteSpace(_site.Url) && UsernameTextBox != null)
            {
                await _credentialStore.RemovePasswordAsync(_site.Url, UsernameTextBox.Text);
            }
            DialogResult = true;
        }
    }
}
