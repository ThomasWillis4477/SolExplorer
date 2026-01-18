using IsoMauiEngine.Engine;
using IsoMauiEngine.Diagnostics;
using IsoMauiEngine.Rendering;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
#endif

namespace IsoMauiEngine;

public partial class MainPage : ContentPage
{
	private readonly GameHost _host;
	private readonly IsoDrawable _drawable;

	public MainPage()
	{
		InitializeComponent();

		_host = new GameHost(Dispatcher);
		_drawable = new IsoDrawable(_host);
		GameView.Drawable = _drawable;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		GameInputRouter.CurrentInput = _host.Input;
		SpriteAssets.EnsureLoaded();
		_host.Start(GameView);
		TryHookKeyboard();
	}

	private void OnPointerPressed(object? sender, PointerEventArgs e)
	{
		var pos = e.GetPosition(GameView);
		if (!pos.HasValue)
		{
			return;
		}
		_host.Navigation.HandleLeftClickScreen(new System.Numerics.Vector2((float)pos.Value.X, (float)pos.Value.Y));
	}

	protected override void OnDisappearing()
	{
		if (ReferenceEquals(GameInputRouter.CurrentInput, _host.Input))
		{
			GameInputRouter.CurrentInput = null;
		}
		_host.Stop();
		base.OnDisappearing();
	}

	private void TryHookKeyboard()
	{
#if WINDOWS
		if (GameView?.Handler?.PlatformView is not UIElement element)
		{
			return;
		}

		element.KeyDown -= OnWindowsKeyDown;
		element.KeyUp -= OnWindowsKeyUp;
		element.KeyDown += OnWindowsKeyDown;
		element.KeyUp += OnWindowsKeyUp;

		element.IsTabStop = true;
		element.Focus(FocusState.Programmatic);
#endif
	}

#if WINDOWS
	private void OnWindowsKeyDown(object sender, KeyRoutedEventArgs e)
	{
		Diagnostics.RouteDebugLogger.Log($"[KeyDown.Page] key={e.Key} repeat={e.KeyStatus.RepeatCount} src={(e.OriginalSource?.GetType().Name ?? "(null)")}");
		_host.Input.SetKey(e.Key, isDown: true);
	}

	private void OnWindowsKeyUp(object sender, KeyRoutedEventArgs e)
	{
		Diagnostics.RouteDebugLogger.Log($"[KeyUp.Page] key={e.Key} repeat={e.KeyStatus.RepeatCount} src={(e.OriginalSource?.GetType().Name ?? "(null)")}");
		_host.Input.SetKey(e.Key, isDown: false);
	}
#endif
}
