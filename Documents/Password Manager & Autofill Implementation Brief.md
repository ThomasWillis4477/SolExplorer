# (01/12/2025 03:39)

# ArsX One-View — Password Manager & Autofill Implementation Brief

## 1. Context

You are extending **ArsX One-View**, a .NET 9 WPF application that wraps **WebView2** as a lightweight multi-site browser with a sidebar of site buttons, navigation toolbar, and an About dialog. A secure credential vault already exists using **Windows DPAPI** (`DpapiCredentialStore` in `Services/`), storing encrypted passwords in `%APPDATA%\ArsX\OneView\vault.bin`.

We now want a **first-pass Password Manager** that:

- Uses the **existing main WebView2 display** (no extra web views).
- Can **store multiple credentials per website** (site + username + password).
- Provides a **clean UI** to view/add/edit/delete entries.
- Can **autofill username + password fields** on the current page, similar to Chrome’s “fill” behaviour, but triggered explicitly (no silent autofill on page load).

Security baseline:  
- Passwords remain **only in the DPAPI-backed store**.  
- A separate metadata file contains non-sensitive info (site name, URL/domain, username).  
- No plaintext password is written to disk.

---

## 2. Goals

1. **Password Manager Window**
   - A dedicated WPF window that displays a table/list of credentials:
     - Site Name
     - Site URL / Domain
     - Username
   - Buttons: **Add**, **Edit**, **Delete**, **Close**.
   - When adding/editing, allow input of **password**; store it securely via DPAPI.

2. **Metadata + Vault Integration**
   - Introduce a small metadata layer that:
     - Persists credential entries (minus passwords) to a JSON file in `%APPDATA%\ArsX\OneView\sites.json`.
     - Uses the existing `ICredentialStore` / `DpapiCredentialStore` for password storage.
   - Map passwords by a deterministic key, e.g. `"{domain}|{username}"`.

3. **Autofill Command Using Main WebView2**
   - Add a toolbar button or menu entry **“Autofill Login”**.
   - When invoked:
     - Detect the current page’s domain from `webView.CoreWebView2.Source`.
     - Let the user choose one of the stored credentials for that domain (if multiple) via a simple dialog or selection.
     - Inject JavaScript into the **current page** to fill:
       - A likely username/email input.
       - A password input.
   - JS should be robust but simple: scan forms and inputs, look for:
     - `input[type=password]` for password.
     - `input[type=text]`, `input[type=email]`, or fields whose `name`/`id` includes `user`, `login`, `email` for username.

4. **Extensibility**
   - Architecture should be ready for later improvements:
     - Per-site autofill settings.
     - “On page load, suggest autofill” behaviour.
     - Search/filter in password manager.

---

## 3. Tasks

### 3.1 Add Credential Model

**File:** `Models/SiteCredential.cs` (new)

Create a simple model for metadata:

- Properties (with `get; set;`):
  - `Guid Id`
  - `string SiteName`
  - `string Domain` (e.g. `discord.com`)
  - `string Url` (optional, e.g. `https://discord.com/app`)
  - `string Username`
- Optional: `string Notes`

This model is **metadata only** — no password field.

### 3.2 Metadata Store for Credentials

**File:** `Services/CredentialMetadataStore.cs` (new)

Create a service class responsible for loading/saving a list of `SiteCredential` objects to a JSON file under `%APPDATA%\ArsX\OneView\sites.json`.

Implementation details:

- On construction:
  - Determine app data directory (e.g. `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArsX", "OneView")`).
  - Ensure directory exists.
  - Load JSON file if present; otherwise start with an empty list.
- Public API (synchronous or async, your choice, but keep consistent):
  - `IReadOnlyList<SiteCredential> GetAll()`
  - `SiteCredential? GetById(Guid id)`
  - `IEnumerable<SiteCredential> GetByDomain(string domain)` (case-insensitive, handle `www.` stripping).
  - `void AddOrUpdate(SiteCredential credential)` (new `Id` if default).
  - `void Remove(Guid id)`
- After any mutation, persist the collection back to JSON.
- Use `System.Text.Json` with sensible options (e.g. `PropertyNameCaseInsensitive = true`).

### 3.3 Extend DPAPI Credential Store for Keyed Passwords

**File:** `Services/ICredentialStore.cs` / `Services/DpapiCredentialStore.cs`

If necessary, extend the interface and implementation to support keyed storage by an arbitrary string:

- Add methods (or adapt existing ones if compatible with current design):
  - `Task StorePasswordAsync(string key, string password);`
  - `Task<string?> RetrievePasswordAsync(string key);`
  - `Task RemovePasswordAsync(string key);`
- Use a deterministic key format like:
  - `string key = $"{domain.ToLowerInvariant()}|{username}";`
- `DpapiCredentialStore` should:
  - Maintain an internal dictionary `Dictionary<string, byte[]>` mapping key → encrypted bytes.
  - Serialize this dictionary to the existing `vault.bin` file.
- If these semantics already exist, just ensure the Password Manager uses them correctly.

### 3.4 Password Manager Window (UI)

**File:** `Views/PasswordManagerWindow.xaml` (new)  
**File:** `Views/PasswordManagerWindow.xaml.cs` (new)

Create a WPF window with:

- Title: `ArsX One-View — Password Manager`
- Minimal size (e.g. 600x400, resizable).
- Layout:
  - `DataGrid` or `ListView` bound to a collection of `SiteCredential` items.
    - Columns: Site, Domain, Username.
  - Below or beside it, buttons:
    - `Add...`
    - `Edit...`
    - `Delete`
    - `Close`

**ViewModel / Code-behind approach (simple):**

- In code-behind, hold an `ObservableCollection<SiteCredential>` loaded from `CredentialMetadataStore`.
- Bind the DataGrid’s `ItemsSource` to this collection.
- Implement `Add`:
  - Open a simple dialog (can be a second small window or WPF `Window` with fields):
    - Site Name
    - Domain
    - URL (optional)
    - Username
    - Password (PasswordBox)
  - On OK:
    - Build the deterministic `key` using domain + username.
    - Store password via `ICredentialStore.StorePasswordAsync(key, password)`.
    - Add a `SiteCredential` to the metadata store and refresh UI.
- Implement `Edit`:
  - Pre-populate fields from the selected entry.
  - Allow changing any field; if username or domain changed, treat this as:
    - Remove old key/password, write new key/password.
  - Optionally offer a “Change password” PasswordBox.
- Implement `Delete`:
  - Confirm, then:
    - Remove metadata entry.
    - Remove password from `ICredentialStore`.

Make sure the Password Manager window is **modal** when invoked from the main window (use `ShowDialog()`), so users complete or cancel their edits cleanly.

### 3.5 Wire the Settings → Password Manager Menu

**File:** `MainWindow.xaml`  
**File:** `MainWindow.xaml.cs`

You already have a settings (three-dots) menu with a stubbed “Password Manager” option.

- Ensure the menu item has a clear name, e.g. `MenuPasswordManager`.
- In `MainWindow.xaml.cs`, implement the click handler:

```csharp
private void PasswordManagerMenu_Click(object sender, RoutedEventArgs e)
{
    var metadataStore = new CredentialMetadataStore();
    var credentialStore = new DpapiCredentialStore();
    var window = new PasswordManagerWindow(metadataStore, credentialStore)
    {
        Owner = this
    };
    window.ShowDialog();
}
```


