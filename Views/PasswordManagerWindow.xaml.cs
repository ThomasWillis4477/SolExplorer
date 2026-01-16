using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using arsX.Sol_Explorer.Models;
using arsX.Sol_Explorer.Services;

namespace arsX.Sol_Explorer.Views
{
    public partial class PasswordManagerWindow : UserControl
    {
        public event EventHandler? RequestClose;
        private readonly CredentialMetadataStore _metadataStore;
        private readonly ICredentialStore _credentialStore;
        private ObservableCollection<SiteCredential> _credentials;

        public PasswordManagerWindow(CredentialMetadataStore metadataStore, ICredentialStore credentialStore)
        {
            InitializeComponent();
            _metadataStore = metadataStore;
            _credentialStore = credentialStore;
            _credentials = new ObservableCollection<SiteCredential>(_metadataStore.GetAll());
            CredentialsGrid.ItemsSource = _credentials;
            CloseButton.Click += (_, __) => RequestClose?.Invoke(this, new RoutedEventArgs());

            AddButton.Click += async (s, e) =>
            {
                try
                {
                    var editWin = new EditCredentialWindow();
                    var result = editWin.ShowDialog();
                    if (result == true)
                    {
                        var cred = editWin.Credential;
                        _metadataStore.AddOrUpdate(cred);
                        await _credentialStore.SetPasswordAsync(cred.SiteName.ToLowerInvariant(), cred.Username, editWin.Password);
                        _credentials.Clear();
                        foreach (var c in _metadataStore.GetAll())
                            _credentials.Add(c);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error adding credential: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            EditButton.Click += async (s, e) =>
            {
                try
                {
                    if (CredentialsGrid.SelectedItem is SiteCredential selected)
                    {
                        var pw = await _credentialStore.GetPasswordAsync(selected.SiteName.ToLowerInvariant(), selected.Username) ?? string.Empty;
                        var editWin = new EditCredentialWindow(selected, pw);
                        var result = editWin.ShowDialog();
                        if (result == true)
                        {
                            var cred = editWin.Credential;
                            if (cred.SiteName.ToLowerInvariant() != selected.SiteName.ToLowerInvariant() || cred.Username != selected.Username)
                            {
                                await _credentialStore.RemovePasswordAsync(selected.SiteName.ToLowerInvariant(), selected.Username);
                            }
                            _metadataStore.AddOrUpdate(cred);
                            await _credentialStore.SetPasswordAsync(cred.SiteName.ToLowerInvariant(), cred.Username, editWin.Password);
                            _credentials.Clear();
                            foreach (var c in _metadataStore.GetAll())
                                _credentials.Add(c);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error editing credential: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            DeleteButton.Click += async (s, e) =>
            {
                try
                {
                    if (CredentialsGrid.SelectedItem is SiteCredential selected)
                    {
                        if (MessageBox.Show($"Delete credential for {selected.SiteName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            _metadataStore.Remove(selected.Id);
                            await _credentialStore.RemovePasswordAsync(selected.SiteName.ToLowerInvariant(), selected.Username);
                            _credentials.Clear();
                            foreach (var c in _metadataStore.GetAll())
                                _credentials.Add(c);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting credential: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }
    }
}
