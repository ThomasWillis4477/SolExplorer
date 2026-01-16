using System.Diagnostics;
using System.ComponentModel;
using IsoMauiEngine.Diagnostics;

namespace IsoMauiEngine;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Navigating += OnShellNavigating;
		Navigated += OnShellNavigated;
		PropertyChanged += OnShellPropertyChanged;

		// Initialize route info at startup.
		TryUpdateCurrentLocation();
		RouteDebugLogger.Log($"[App] Started. LogFile={RouteDebugLogger.LogFilePath}");
	}

	private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Some platforms don't reliably fire Navigated/Navigating for all selection changes.
		// This is a second channel to see if Shell state is changing.
		if (e.PropertyName is nameof(CurrentState) or nameof(CurrentItem) or nameof(CurrentPage))
		{
			TryUpdateCurrentLocation();
			RouteDebugLogger.Log($"[Shell.PropertyChanged] {e.PropertyName} Current={RouteDebugState.Instance.CurrentLocation}");
		}
	}

	private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
	{
		var current = e.Current?.Location.ToString() ?? "(null)";
		var target = e.Target?.Location.ToString() ?? "(null)";
		RouteDebugState.Instance.LastNavigating = target;
		Debug.WriteLine($"[Shell.Navigating] Source={e.Source} Current={current} Target={target}");
		RouteDebugLogger.Log($"[Shell.Navigating] Source={e.Source} Current={current} Target={target}");
	}

	private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
	{
		RouteDebugState.Instance.LastNavigated = e.Current?.Location.ToString() ?? "(null)";
		TryUpdateCurrentLocation();
		Debug.WriteLine($"[Shell.Navigated] Current={RouteDebugState.Instance.CurrentLocation}");
		RouteDebugLogger.Log($"[Shell.Navigated] Current={RouteDebugState.Instance.CurrentLocation}");
	}

	private void TryUpdateCurrentLocation()
	{
		try
		{
			RouteDebugState.Instance.CurrentLocation = CurrentState?.Location.ToString() ?? "(unknown)";
		}
		catch
		{
			// Ignore: CurrentState may not be ready early in startup.
		}
	}
}
