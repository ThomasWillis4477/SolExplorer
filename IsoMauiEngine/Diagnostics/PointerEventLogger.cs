using System.Diagnostics;

namespace IsoMauiEngine.Diagnostics;

public static class PointerEventLogger
{
	private static int _attached;

	public static void TryAttachToWindow(Microsoft.Maui.Controls.Window window)
	{
		if (Interlocked.Exchange(ref _attached, 1) == 1)
		{
			return;
		}

		try
		{
			RouteDebugLogger.Log("[Pointer] Attaching pointer logger...");
		}
		catch
		{
			// ignore
		}

#if WINDOWS
		try
		{
			// The platform window for MAUI on Windows.
			var platformWindow = window.Handler?.PlatformView;

			// Fall back: retry once the handler is created.
			if (platformWindow is null)
			{
				window.HandlerChanged += (_, _) =>
				{
					try
					{
						AttachWindows(window);
					}
					catch (Exception ex)
					{
						RouteDebugLogger.Log($"[Pointer] Attach failed: {ex.Message}");
					}
				};
				return;
			}

			AttachWindows(window);
		}
		catch (Exception ex)
		{
			RouteDebugLogger.Log($"[Pointer] Attach failed: {ex.Message}");
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

		// window.Handler.PlatformView is a Microsoft.Maui.MauiWinUIWindow.
		// We attach to its Content so we get events across the whole app surface.
		if (platformView is not Microsoft.Maui.MauiWinUIWindow mauiWindow)
		{
			RouteDebugLogger.Log($"[Pointer] Unexpected platform window type: {platformView.GetType().FullName}");
			return;
		}

		if (mauiWindow.Content is not Microsoft.UI.Xaml.UIElement root)
		{
			RouteDebugLogger.Log("[Pointer] Platform window content was not a UIElement.");
			return;
		}

		root.PointerPressed += (_, e) =>
		{
			var p = e.GetCurrentPoint(root);
			var src = e.OriginalSource;
			string srcInfo;
			try
			{
				if (src is Microsoft.UI.Xaml.FrameworkElement fe)
				{
					srcInfo = $"{fe.GetType().Name}(Name='{fe.Name}')";
				}
				else
				{
					srcInfo = src?.GetType().Name ?? "(null)";
				}
			}
			catch
			{
				srcInfo = "(unavailable)";
			}

			RouteDebugLogger.Log($"[PointerPressed] x={p.Position.X:0.0} y={p.Position.Y:0.0} device={p.PointerDeviceType} buttons=L:{p.Properties.IsLeftButtonPressed} R:{p.Properties.IsRightButtonPressed} M:{p.Properties.IsMiddleButtonPressed} source={srcInfo}");
		};

		root.PointerReleased += (_, e) =>
		{
			var p = e.GetCurrentPoint(root);
			RouteDebugLogger.Log($"[PointerReleased] x={p.Position.X:0.0} y={p.Position.Y:0.0} device={p.PointerDeviceType}");
		};

		root.PointerWheelChanged += (_, e) =>
		{
			var p = e.GetCurrentPoint(root);
			RouteDebugLogger.Log($"[PointerWheel] x={p.Position.X:0.0} y={p.Position.Y:0.0} delta={p.Properties.MouseWheelDelta}");
		};

		root.Tapped += (_, e) =>
		{
			var pos = e.GetPosition(root);
			RouteDebugLogger.Log($"[Tapped] x={pos.X:0.0} y={pos.Y:0.0}");
		};

		RouteDebugLogger.Log("[Pointer] Attached to WinUI root element.");
	}
#endif
}
