# COPILOT PATCH — MainWindow Stage 3.2
# Goal: Restore JSON-driven sidebar and initial navigation after Stage 4

## Context

ArsX One-View currently:
- Uses `Config/sites.json` + `SiteProfile` + `SiteProfileStore`.
- Has `MainWindow.xaml` defining:
  - `StackPanel x:Name="Sidebar"`
  - `Button x:Name="BackButton"`
  - `Button x:Name="ForwardButton"`
  - `Button x:Name="ReloadButton"`
  - `TextBox x:Name="AddressBar"`
  - `WebView2 x:Name="Browser"`

But `MainWindow.xaml.cs` still contains the older Stage 2 implementation that:
- Does NOT use `SiteProfileStore`.
- Does NOT rebuild the sidebar from JSON.
- Only partially handles navigation.

We want a clean Stage 3/4–compatible implementation.

## Task for Copilot

Open `MainWindow.xaml.cs` and **replace its entire contents** with the following code.  
Do not change namespaces, XAML file names, or any other files.

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ArsX.OneView.Models;
using ArsX.OneView.Services;
using Microsoft.Web.WebView2.Core;

namespace ArsX.OneView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SiteProfileStore _siteStore;
        private List<SiteProfile> _sites = new();

        public MainWindow()
        {
            InitializeComponent();

            _siteStore = new SiteProfileStore();

            // Build sidebar buttons based on Config/sites.json
            LoadSitesAndBuildSidebar();

            // Toolbar navigation
            BackButton.Click    += (s, e) => { if (Browser.CanGoBack)    Browser.GoBack(); };
            ForwardButton.Click += (s, e) => { if (Browser.CanGoForward) Browser.GoForward(); };
            ReloadButton.Click  += (s, e) => Browser.Reload();
            AddressBar.KeyDown  += AddressBar_KeyDown;

            // WebView2 events
            Browser.NavigationCompleted += Browser_NavigationCompleted;

            // Initial navigation:
            // Prefer AddressBar.Text if set, then first site URL, otherwise Discord.
            var startUrl =
                !string.IsNullOrWhiteSpace(AddressBar.Text)
                    ? AddressBar.Text
                    : (_sites.Count > 0 && !string.IsNullOrWhiteSpace(_sites[0].Url)
                        ? _sites[0].Url
                        : "https://discord.com/app");

            NavigateTo(startUrl);
        }

        private void LoadSitesAndBuildSidebar()
        {
            Sidebar.Children.Clear();

            try
            {
                _sites = _siteStore.GetSites();
            }
            catch
            {
                _sites = new List<SiteProfile>();
            }

            foreach (var site in _sites)
            {
                if (string.IsNullOrWhiteSpace(site.Url))
                    continue;

                var btn = new Button
                {
                    Content = string.IsNullOrWhiteSpace(site.Short) ? site.Name : site.Short,
                    Tag = site.Url,
                    Height = 40,
                    Margin = new Thickness(8, 4, 8, 4),
                    Foreground = Brushes.White
                };

                // Background colour from JSON with safe fallback
                if (!string.IsNullOrWhiteSpace(site.Color))
                {
                    try
                    {
                        btn.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(site.Color);
                    }
                    catch
                    {
                        btn.Background = Brushes.DimGray;
                    }
                }
                else
                {
                    btn.Background = Brushes.DimGray;
                }

                btn.Click += (s, e) =>
                {
                    if (btn.Tag is string url && !string.IsNullOrWhiteSpace(url))
                    {
                        NavigateTo(url);
                    }
                };

                Sidebar.Children.Add(btn);
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = AddressBar.Text;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    NavigateTo(url);
                }
            }
        }

        private void NavigateTo(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            var url = input.Trim();

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (url.Contains('.'))
                {
                    url = "https://" + url;
                }
                else
                {
                    url = "https://duckduckgo.com/?q=" + Uri.EscapeDataString(url);
                }
            }

            try
            {
                Browser.Source = new Uri(url);
                AddressBar.Text = url;
            }
            catch
            {
                MessageBox.Show(
                    "Unable to navigate to: " + url,
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess || Browser.CoreWebView2 == null)
                return;

            Title = Browser.CoreWebView2.DocumentTitle;
            AddressBar.Text = Browser.Source?.ToString() ?? AddressBar.Text;
        }
    }
}
