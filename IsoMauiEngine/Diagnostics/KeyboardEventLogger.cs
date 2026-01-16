using System.Diagnostics;

namespace IsoMauiEngine.Diagnostics;

public static class KeyboardEventLogger
{
	private static int _attached;

	public static void TryAttachToWindow(Microsoft.Maui.Controls.Window window)
	{
		if (Interlocked.Exchange(ref _attached, 1) == 1)
		{
			return;
		}

#if WINDOWS
		try
		{
			RouteDebugLogger.Log("[Keyboard] Attaching keyboard logger...");
		}
		catch
		{
			// ignore
		}

		try
		{
			var platformWindow = window.Handler?.PlatformView;
			if (platformWindow is null)
			{
				window.HandlerChanged += (_, _) =>
				{
					try { AttachWindows(window); }
					catch (Exception ex) { RouteDebugLogger.Log($"[Keyboard] Attach failed: {ex.Message}"); }
				};
				return;
			}

			AttachWindows(window);
		}
		catch (Exception ex)
		{
			RouteDebugLogger.Log($"[Keyboard] Attach failed: {ex.Message}");
			Debug.WriteLine(ex);
		}
#endif
	}

#if WINDOWS
	private static void AttachWindows(Microsoft.Maui.Controls.Window window)
	{
		var platformView = window.Handler?.PlatformView;
		if (platformView is null)
		{
			return;
		}

		if (platformView is not Microsoft.Maui.MauiWinUIWindow mauiWindow)
		{
			RouteDebugLogger.Log($"[Keyboard] Unexpected platform window type: {platformView.GetType().FullName}");
			return;
		}

		if (mauiWindow.Content is not Microsoft.UI.Xaml.UIElement root)
		{
			RouteDebugLogger.Log("[Keyboard] Platform window content was not a UIElement.");
			return;
		}

		mauiWindow.Activated += (_, e) =>
		{
			RouteDebugLogger.Log($"[Keyboard] Window.Activated State={e.WindowActivationState}");
			TryFocus(root, reason: "Window.Activated");
		};

		root.GotFocus += (_, e) =>
		{
			RouteDebugLogger.Log($"[Keyboard] GotFocus src={DescribeElement(e.OriginalSource)}");
		};

		root.LostFocus += (_, e) =>
		{
			RouteDebugLogger.Log($"[Keyboard] LostFocus src={DescribeElement(e.OriginalSource)}");
		};

		root.PointerPressed += (_, e) =>
		{
			// Clicking anywhere should put focus back into the app surface.
			TryFocus(root, reason: "PointerPressed");
		};

		root.KeyDown += (_, e) =>
		{
			RouteDebugLogger.Log($"[KeyDown] key={e.Key} status={e.KeyStatus.RepeatCount}/{e.KeyStatus.ScanCode} ext={e.KeyStatus.IsExtendedKey} rel={e.KeyStatus.IsKeyReleased} menu={e.KeyStatus.IsMenuKeyDown} src={DescribeElement(e.OriginalSource)}");

			try
			{
				GameInputRouter.CurrentInput?.SetKey(e.Key, isDown: true);
			}
			catch (Exception ex)
			{
				RouteDebugLogger.Log($"[KeyDown] Input forward failed: {ex.Message}");
			}
		};

		root.KeyUp += (_, e) =>
		{
			RouteDebugLogger.Log($"[KeyUp] key={e.Key} status={e.KeyStatus.RepeatCount}/{e.KeyStatus.ScanCode} ext={e.KeyStatus.IsExtendedKey} rel={e.KeyStatus.IsKeyReleased} menu={e.KeyStatus.IsMenuKeyDown} src={DescribeElement(e.OriginalSource)}");

			try
			{
				GameInputRouter.CurrentInput?.SetKey(e.Key, isDown: false);
			}
			catch (Exception ex)
			{
				RouteDebugLogger.Log($"[KeyUp] Input forward failed: {ex.Message}");
			}
		};

		TryFocus(root, reason: "AttachWindows");
		RouteDebugLogger.Log("[Keyboard] Attached to WinUI root element.");
	}

	private static void TryFocus(Microsoft.UI.Xaml.UIElement root, string reason)
	{
		try
		{
			root.IsTabStop = true;
			var focused = root.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
			RouteDebugLogger.Log($"[Keyboard] Focus({reason}) result={focused}");
		}
		catch (Exception ex)
		{
			RouteDebugLogger.Log($"[Keyboard] Focus({reason}) failed: {ex.Message}");
		}
	}

	private static string DescribeElement(object? element)
	{
		try
		{
			if (element is Microsoft.UI.Xaml.FrameworkElement fe)
			{
				var name = string.IsNullOrWhiteSpace(fe.Name) ? "" : $" Name='{fe.Name}'";
				var automationId = Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(fe);
				var aid = string.IsNullOrWhiteSpace(automationId) ? "" : $" AutomationId='{automationId}'";
				return $"{fe.GetType().Name}({name}{aid})";
			}

			return element?.GetType().Name ?? "(null)";
		}
		catch
		{
			return "(unavailable)";
		}
	}
#endif
}
