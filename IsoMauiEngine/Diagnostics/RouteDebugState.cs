using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IsoMauiEngine.Diagnostics;

public sealed class RouteDebugState : INotifyPropertyChanged
{
	public static RouteDebugState Instance { get; } = new();

	private string _lastNavigating = "";
	private string _lastNavigated = "";
	private string _currentLocation = "";

	public event PropertyChangedEventHandler? PropertyChanged;

	public string LastNavigating
	{
		get => _lastNavigating;
		set
		{
			if (value == _lastNavigating) return;
			_lastNavigating = value;
			OnPropertyChanged();
		}
	}

	public string LastNavigated
	{
		get => _lastNavigated;
		set
		{
			if (value == _lastNavigated) return;
			_lastNavigated = value;
			OnPropertyChanged();
		}
	}

	public string CurrentLocation
	{
		get => _currentLocation;
		set
		{
			if (value == _currentLocation) return;
			_currentLocation = value;
			OnPropertyChanged();
		}
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
