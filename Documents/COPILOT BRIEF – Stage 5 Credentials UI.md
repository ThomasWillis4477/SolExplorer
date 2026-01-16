# COPILOT BRIEF — ArsX One-View — Stage 5
# Goal: Username + Password storage for sidebar sites, using existing DPAPI vault
# RULE: Do NOT modify or overwrite any existing markdown files.

## CONTEXT

ArsX One-View currently has:

- JSON-driven sidebar via:
  - Config/sites.json
  - Models/SiteProfile
  - Services/SiteProfileStore (static)

- WebView2-based browser window:
  - MainWindow.xaml defines:
    - StackPanel x:Name="Sidebar"
    - WebView2 x:Name="Browser"
    - Toolbar with Back/Forward/Reload/AddressBar

- Vault backend from Stage 4:
  - Services/ICredentialStore
  - Services/DpapiCredentialStore
  - App exposes:
    - public static ICredentialStore CredentialStore { get; } = new DpapiCredentialStore();

We now want a **simple UI** to store credentials for a site:

- Per-site **username**
- Per-site **password**, encrypted with DPAPI via `ICredentialStore`

We are NOT doing auto-login or auto-fill yet.  
Just: **“store and recall credentials per site”** via a small dialog.

---

## HIGH-LEVEL DESIGN

- Add a **“Manage Credentials…”** dialog window.
- Open it by **right-clicking a sidebar button**.
- Dialog shows:
  - Site name (read-only)
  - URL (read-only)
  - Username (editable `TextBox`, prefilled from `SiteProfile.Username`)
  - Password (editable `PasswordBox`, prefilled from vault if present)
  - Buttons: **Save**, **Clear**, **Cancel**

- When clicking **Save**:
  - Update `SiteProfile.Username` via `SiteProfileStore.UpdateUsername(...)` and `SiteProfileStore.SaveSites()`.
  - Store password in vault:
    - Key: site URL
    - Username: same as `SiteProfile.Username`
    - Value: password in `PasswordBox`

- When clicking **Clear**:
  - Clear username from `SiteProfile` and resave sites.json.
  - Remove password for that `(siteKey, username)` from the vault.
  - Close dialog.

---

## 1. NEW WINDOW: ManageCredentialsWindow

Create new WPF window:
- File: `ManageCredentialsWindow.xaml`
- Code-behind: `ManageCredentialsWindow.xaml.cs`
- Namespace: `ArsX.OneView`

### XAML requirements

`ManageCredentialsWindow.xaml`:

- `Window` title: `"Manage Credentials"`
- Layout can be simple Grid or StackPanel.

Controls:

- Read-only labels:
  - Site name
  - Site URL

- `TextBox`:
  - `x:Name="UsernameTextBox"`

- `PasswordBox`:
  - `x:Name="PasswordBox"`

- Buttons:
  - `x:Name="SaveButton"`, Content `"Save"`
  - `x:Name="ClearButton"`, Content `"Clear"`
  - `x:Name="CancelButton"`, Content `"Cancel"`

Basic spacing/margins only; styling can be minimal.

### Code-behind requirements

`ManageCredentialsWindow.xaml.cs`:

Constructor signature:

```csharp
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

        // Prefill username from SiteProfile
        UsernameTextBox.Text = _site.Username ?? string.Empty;

        // Async load password from vault
        Loaded += async (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(_site.Url) &&
                !string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                var pw = await _credentialStore.GetPasswordAsync(_site.Url, UsernameTextBox.Text);
                if (!string.IsNullOrEmpty(pw))
                {
                    PasswordBox.Password = pw;
                }
            }
        };

        SaveButton.Click   += SaveButton_Click;
        ClearButton.Click  += ClearButton_Click;
        CancelButton.Click += (_, __) => DialogResult = false;
    }
}
