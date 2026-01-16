# COPILOT BRIEF — ArsX One-View — Stage 4
# Purpose: Implement secure-ish ArsX Vault using Windows DPAPI (passwords encrypted at rest)
# RULE: Do NOT modify or overwrite any existing markdown files.

## HIGH-LEVEL GOAL

Extend ArsX One-View with a **local credential vault** that can store and retrieve passwords **securely on Windows** using **DPAPI** (`System.Security.Cryptography.ProtectedData`).

Constraints:

- Secrets are **never stored in plaintext**.
- Secrets are **never stored in JSON** or in `sites.json`.
- Vault is local to the current user profile.
- Vault is implemented as a **service layer only** in this stage (no full UI yet).

---

## ENVIRONMENT

- Project: `ArsX.OneView`
- Language: C#
- Framework: .NET 9
- UI: WPF
- Existing components:
  - `Models/SiteProfile` with `Username`
  - `Services/SiteProfileStore` with `Config/sites.json`
  - `MainWindow` with sidebar + WebView2 + navigation

---

## OVERALL DESIGN

Implement a **credential store abstraction** and a **DPAPI-backed implementation**:

- Interface: `ICredentialStore`
- Implementation: `DpapiCredentialStore`

The vault will:

- Use a single encrypted file under `%APPDATA%\ArsX\OneView\vault.bin`
- Store multiple credentials (site + username + password), all encrypted
- Use **Windows DPAPI** with `DataProtectionScope.CurrentUser`
- Handle serialization in-memory only; the persisted file contains only encrypted bytes

---

## REQUIRED NEW FILES

### 1. Add file: `Services/ICredentialStore.cs`

Define an interface like:

```csharp
using System.Threading.Tasks;

namespace ArsX.OneView.Services
{
    public interface ICredentialStore
    {
        Task SetPasswordAsync(string siteKey, string username, string password);
        Task<string?> GetPasswordAsync(string siteKey, string username);
        Task RemovePasswordAsync(string siteKey, string username);
    }
}
