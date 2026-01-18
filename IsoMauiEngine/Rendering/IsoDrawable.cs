using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Navigation;
using Microsoft.Maui.Graphics;

namespace IsoMauiEngine.Rendering;

public sealed class IsoDrawable : IDrawable
{
	private readonly GameHost _host;
	private readonly List<DrawItem> _allItems = new(capacity: 2048);
	private readonly List<DrawItem> _tileItems = new(capacity: 2048);
	private readonly List<DrawItem> _entityItems = new(capacity: 512);

	public IsoDrawable(GameHost host)
	{
		_host = host;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.ResetState();
		canvas.FillColor = Color.FromArgb("#0B0F14");
		canvas.FillRectangle(dirtyRect);

		// Screen-space world background (does not pan/zoom with the camera).
		var bg = SpriteAssets.WorldBackground;
		if (bg is not null)
		{
			DrawBackgroundCover(canvas, dirtyRect, bg, alpha: 0.9f);
		}

		_host.Camera.ScreenCenter = new Vector2(dirtyRect.Width * 0.5f, dirtyRect.Height * 0.35f);

		_allItems.Clear();
		_tileItems.Clear();
		_entityItems.Clear();

		_host.World.AppendDrawItems(_allItems);
		for (var i = 0; i < _allItems.Count; i++)
		{
			var item = _allItems[i];
			if (item.Type == DrawItemType.Tile)
			{
				_tileItems.Add(item);
			}
			else
			{
				_entityItems.Add(item);
			}
		}

		// Ground pass: tiles never occlude entities.
		_tileItems.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
		for (var i = 0; i < _tileItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _tileItems[i]);
		}

		// Entity pass: depth sort entities by their feet (SortY) + optional HeightBias/LayerBias.
		_entityItems.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
		for (var i = 0; i < _entityItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _entityItems[i]);
		}

		// Simple debug HUD
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12;
		canvas.DrawString($"Items: {_allItems.Count}", 8, 8, HorizontalAlignment.Left);

		if (_host.Input.DebugOverlayEnabled)
		{
			DrawNavigationDebug(canvas);
			DrawModuleDebugOverlay(canvas);
		}
	}

	private void DrawNavigationDebug(ICanvas canvas)
	{
		var nav = _host.Navigation;
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12;
		canvas.DrawString($"Mode: {nav.CurrentMode}", 8, 26, HorizontalAlignment.Left);
		canvas.DrawString($"Navigator: {nav.ActiveNavigator}", 8, 42, HorizontalAlignment.Left);

		var lineY = 58f;
		var path = nav.CurrentPath;
		if (path is not null && !string.IsNullOrWhiteSpace(path.DebugInfo))
		{
			canvas.DrawString(path.DebugInfo, 8, lineY, HorizontalAlignment.Left);
			lineY += 16f;
		}

		var locationText = "Location: In Space";
		if (_host.World.TryFindContainingModule(_host.World.Player.WorldPos, out var m, out _))
		{
			locationText = m.IsAirlock
				? $"Location: Airlock #{m.ModuleId}"
				: $"Location: #{m.ModuleId}";
		}
		canvas.DrawString(locationText, 8, lineY, HorizontalAlignment.Left);

		if (path is null || !path.IsValid || path.Waypoints.Count == 0)
		{
			return;
		}

		// Draw waypoint polyline in screen-space.
		canvas.StrokeColor = Color.FromArgb("#FFD166");
		canvas.StrokeSize = 2;
		for (var i = 0; i < path.Waypoints.Count - 1; i++)
		{
			var a = _host.Camera.WorldToScreen(path.Waypoints[i]);
			var b = _host.Camera.WorldToScreen(path.Waypoints[i + 1]);
			canvas.DrawLine(a.X, a.Y, b.X, b.Y);
		}

		canvas.FillColor = Color.FromArgb("#FFD166");
		for (var i = 0; i < path.Waypoints.Count; i++)
		{
			var p = _host.Camera.WorldToScreen(path.Waypoints[i]);
			canvas.FillCircle(p.X, p.Y, 4 * _host.Camera.Zoom);
		}

		// DebugInfo and location are drawn above.
	}

	private static void DrawBackgroundCover(ICanvas canvas, RectF viewport, Microsoft.Maui.Graphics.IImage image, float alpha)
	{
		var iw = MathF.Max(1f, image.Width);
		var ih = MathF.Max(1f, image.Height);
		var vw = MathF.Max(1f, viewport.Width);
		var vh = MathF.Max(1f, viewport.Height);

		// Aspect-fill (cover) so there are no empty bars.
		var scale = MathF.Max(vw / iw, vh / ih);
		var dw = iw * scale;
		var dh = ih * scale;
		var dx = viewport.X + (vw - dw) * 0.5f;
		var dy = viewport.Y + (vh - dh) * 0.5f;

		canvas.Alpha = Math.Clamp(alpha, 0f, 1f);
		canvas.DrawImage(image, dx, dy, dw, dh);
		canvas.Alpha = 1f;
	}

	private void DrawModuleDebugOverlay(ICanvas canvas)
	{
		var modules = _host.World.Modules;
		var graph = _host.World.ModuleGraph;

		// Door link lines first (so the module/door markers draw above them).
		var links = graph.EnumerateUniqueLinksSnapshot();
		canvas.StrokeColor = Color.FromArgb("#2EC4B6");
		canvas.StrokeSize = 2;
		for (var i = 0; i < links.Count; i++)
		{
			var (a, b) = links[i];
			var ma = _host.World.GetModuleById(a.ModuleId);
			var mb = _host.World.GetModuleById(b.ModuleId);
			if (ma is null || mb is null)
			{
				continue;
			}

			var pa = _host.Camera.WorldToScreen(ma.GetDoorWorldPos(a.Side));
			var pb = _host.Camera.WorldToScreen(mb.GetDoorWorldPos(b.Side));
			canvas.DrawLine(pa.X, pa.Y, pb.X, pb.Y);
		}

		for (var mi = 0; mi < modules.Count; mi++)
		{
			var module = modules[mi];
			var blueprint = module.Blueprint;

			canvas.StrokeColor = module.IsDerelict
				? Color.FromArgb("#BFC0C0")
				: Color.FromArgb("#5AA9E6");
			canvas.StrokeSize = 2;

			// Outline (diamond) using the 4 grid corners.
			var tl = new Vector2(module.OriginX, module.OriginY);
			var tr = new Vector2(module.OriginX + module.Width - 1, module.OriginY);
			var br = new Vector2(module.OriginX + module.Width - 1, module.OriginY + module.Height - 1);
			var bl = new Vector2(module.OriginX, module.OriginY + module.Height - 1);

			var pTl = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)tl.X, (int)tl.Y) + module.WorldOffset);
			var pTr = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)tr.X, (int)tr.Y) + module.WorldOffset);
			var pBr = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)br.X, (int)br.Y) + module.WorldOffset);
			var pBl = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)bl.X, (int)bl.Y) + module.WorldOffset);

			canvas.DrawLine(pTl.X, pTl.Y, pTr.X, pTr.Y);
			canvas.DrawLine(pTr.X, pTr.Y, pBr.X, pBr.Y);
			canvas.DrawLine(pBr.X, pBr.Y, pBl.X, pBl.Y);
			canvas.DrawLine(pBl.X, pBl.Y, pTl.X, pTl.Y);

			// Module label.
			var center = _host.Camera.WorldToScreen(module.GetWorldCenter());
			canvas.FontSize = 10;
			canvas.FontColor = module.IsDerelict ? Color.FromArgb("#FF6B6B") : Colors.White;
			var label = module.IsDerelict ? $"#{module.ModuleId} DERELICT" : $"#{module.ModuleId}";
			canvas.DrawString(label, center.X - 90, center.Y - 18, 180, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

			// Door positions (color by link state).
			canvas.StrokeSize = 2;
			foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
			{
				var linked = graph.TryGetLink(module.ModuleId, side, out _);
				// Linked = docked door (green). Airlock doors are walkable even when unlinked (blue).
				canvas.StrokeColor = linked
					? Color.FromArgb("#06D6A0")
					: (module.IsAirlock ? Color.FromArgb("#4D96FF") : Colors.Orange);
				var p = _host.Camera.WorldToScreen(module.GetDoorWorldPos(side));
				canvas.DrawCircle(p.X, p.Y, 6 * _host.Camera.Zoom);
			}
		}
	}
}
