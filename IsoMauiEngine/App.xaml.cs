using Microsoft.Extensions.DependencyInjection;
using IsoMauiEngine.Diagnostics;

namespace IsoMauiEngine;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		PointerEventLogger.TryAttachToWindow(window);
		KeyboardEventLogger.TryAttachToWindow(window);
		return window;
	}
}