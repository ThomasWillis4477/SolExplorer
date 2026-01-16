# COPILOT BRIEF — ArsX One-View — Stage 3
# Purpose: Add JSON-driven sidebar + ArsX Vault (username only)
# RULE: Do NOT modify or overwrite any existing markdown files.

## OBJECTIVE
Convert the hard-coded sidebar buttons into a data-driven system using a JSON config file.
Introduce the ArsX Vault foundation by storing **usernames only** inside the site profiles.
Do NOT create or store passwords. Password handling will be implemented later via DPAPI.

---

## REQUIRED NEW FILES

### 1. Create folder: Config
### Add file: Config/sites.json

Populate it with the following structure:

[
  {
    "name": "Discord",
    "short": "D",
    "url": "https://discord.com/app",
    "color": "#5865F2",
    "username": null
  },
  {
    "name": "Facebook",
    "short": "f",
    "url": "https://www.facebook.com",
    "color": "#1877F2",
    "username": null
  },
  {
    "name": "X",
    "short": "X",
    "url": "https://x.com",
    "color": "#000000",
    "username": null
  },
  {
    "name": "Yahoo Mail",
    "short": "Y!",
    "url": "https://mail.yahoo.com",
    "color": "#6001D2",
    "username": null
  },
  {
    "name": "Gmail",
    "short": "G",
    "url": "https://mail.google.com",
    "color": "#DB4437",
    "username": null
  }
]

---

## REQUIRED NEW CLASSES

### 2. Add file: Models/SiteProfile.cs

namespace ArsX.OneView.Models
{
    public class SiteProfile
    {
        public string Name { get; set; }
        public string Short { get; set; }
        public string Url { get; set; }
        public string Color { get; set; }
        public string? Username { get; set; }
    }
}

---

### 3. Add file: Services/SiteProfileStore.cs

Requirements for this class:

- Load JSON file Config/sites.json at startup.
- Deserialize into List<SiteProfile>.
- Provide public methods:
    List<SiteProfile> GetSites()
    void UpdateUsername(string siteName, string newUsername)
    void SaveSites()

- SaveSites() should rewrite the JSON file to disk.
- Do NOT add any password fields or password handling.

---

## REQUIRED UPDATES TO MAINWINDOW.XAML

- Remove ALL hard-coded sidebar <Button> elements.
- Keep the <StackPanel x:Name="Sidebar"> intact.
- The sidebar must be initially empty so buttons can be generated dynamically.

---

## REQUIRED UPDATES TO MAINWINDOW.XAML.CS

Modify the constructor or initialization logic so that:

1. Load all sites:
       var sites = SiteProfileStore.LoadSites() or equivalent method.

2. For each SiteProfile, dynamically create a sidebar button:
       var btn = new Button
       {
           Content = site.Short,
           Tag = site.Url,
           Background = (SolidColorBrush)(new BrushConverter().ConvertFromString(site.Color)),
           Foreground = Brushes.White,
           Height = 40,
           Margin = new Thickness(8, 4, 8, 4)
       };
       btn.Click += (s, e) => Navigate(site.Url);
       Sidebar.Children.Add(btn);

3. Leave all toolbar navigation logic unchanged.

---

## ARSX VAULT FOUNDATION — Stage 3

- The "vault" at this stage is simply the Username field inside each site profile.
- Add support in SiteProfileStore for updating usernames.
- Do NOT add passwords, encryption, or credential management yet.

---

## RESTRICTIONS

- DO NOT overwrite ANY existing markdown documentation.
- DO NOT add any password fields.
- DO NOT change existing WebView2 or toolbar logic.
- ONLY modify XAML to remove hard-coded sidebar buttons.
- ALL new work must compile cleanly and keep Stage 2 functionality working.

---

## DELIVERY EXPECTATION

Copilot must generate:

1. Config/sites.json  
2. Models/SiteProfile.cs  
3. Services/SiteProfileStore.cs  
4. Updated MainWindow.xaml (sidebar cleaned)  
5. Updated MainWindow.xaml.cs (dynamic sidebar)  

After generation, the application should run normally with a JSON-powered sidebar.

END OF BRIEF
