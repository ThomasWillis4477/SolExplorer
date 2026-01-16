using System.Windows;
using arsX.Sol_Explorer.Models;
using arsX.Sol_Explorer.Services;

namespace arsX.Sol_Explorer.Views
{
    public partial class EditCredentialWindow : Window
    {
        public SiteCredential Credential { get; private set; }
        public string Password { get; private set; } = string.Empty;

        public EditCredentialWindow(SiteCredential? credential = null, string password = "")
        {
            InitializeComponent();
            Credential = credential ?? new SiteCredential();
            SiteNameTextBox.Text = Credential.SiteName;
            UrlTextBox.Text = Credential.Url;
            UsernameTextBox.Text = Credential.Username;
            PasswordBox.Text = password ?? string.Empty;
            OkButton.Click += OkButton_Click;
            CancelButton.Click += (_, __) => DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Credential.SiteName = SiteNameTextBox.Text;
            Credential.Url = UrlTextBox.Text;
            Credential.Username = UsernameTextBox.Text;
            Password = PasswordBox.Text;
            DialogResult = true;
        }
    }
}
