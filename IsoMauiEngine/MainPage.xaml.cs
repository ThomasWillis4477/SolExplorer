using IsoMauiEngine.Engine;
using IsoMauiEngine.Diagnostics;
using IsoMauiEngine.Navigation;
using IsoMauiEngine.Rendering;
using IsoMauiEngine.World.Modules;

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
	private InteractionMenuRequest? _activeMenu;

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
		_host.Navigation.InteractionMenuRequested -= OnInteractionMenuRequested;
		_host.Navigation.InteractionMenuRequested += OnInteractionMenuRequested;
		_host.Start(GameView);
		TryHookKeyboard();
	}

	private void OnPointerPressed(object? sender, PointerEventArgs e)
	{
		if (InteractionOverlay?.IsVisible == true)
		{
			return;
		}

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
		_host.Navigation.InteractionMenuRequested -= OnInteractionMenuRequested;
		_host.Stop();
		base.OnDisappearing();
	}

	private void OnInteractionMenuRequested(InteractionMenuRequest request)
	{
		_activeMenu = request;
		InteractionOverlay.IsVisible = true;

		InteractionSubtitle.Text = $"Module #{request.ModuleId}";
		switch (request.Kind)
		{
			case CellKind.Locker:
				InteractionTitle.Text = "Locker";
				InteractionPrimaryButton.Text = _host.World.Player.IsSuitEquipped ? "Remove Suit" : "Equip Suit";
				break;

			case CellKind.RcsControl:
				InteractionTitle.Text = "RCS Console";
				InteractionPrimaryButton.Text = _host.World.RcsModeModule?.ModuleId == request.ModuleId
					? "Exit RCS Mode"
					: "Enter RCS Mode";
				break;

			default:
				InteractionTitle.Text = "Interact";
				InteractionPrimaryButton.Text = "Action";
				break;
		}
	}

	private void CloseInteractionMenu()
	{
		InteractionOverlay.IsVisible = false;
		_activeMenu = null;
	}

	private void OnInteractionCancelClicked(object? sender, EventArgs e)
	{
		CloseInteractionMenu();
	}

	private void OnInteractionPrimaryClicked(object? sender, EventArgs e)
	{
		if (!_activeMenu.HasValue)
		{
			CloseInteractionMenu();
			return;
		}

		var req = _activeMenu.Value;
		switch (req.Kind)
		{
			case CellKind.Locker:
				_host.World.Player.IsSuitEquipped = !_host.World.Player.IsSuitEquipped;
				break;

			case CellKind.RcsControl:
				var module = _host.World.GetModuleById(req.ModuleId);
				if (_host.World.RcsModeModule?.ModuleId == req.ModuleId)
				{
					_host.Navigation.SetRcsModeModule(null);
				}
				else
				{
					_host.Navigation.SetRcsModeModule(module);
				}
				break;
		}

		CloseInteractionMenu();
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
