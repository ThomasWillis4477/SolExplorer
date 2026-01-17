using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
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
			DrawModuleDebugOverlay(canvas);
		}
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
		for (var mi = 0; mi < modules.Count; mi++)
		{
			var module = modules[mi];
			var blueprint = module.Blueprint;

			canvas.StrokeColor = Color.FromArgb("#5AA9E6");
			canvas.StrokeSize = 2;

			// Outline (diamond) using the 4 grid corners.
			var tl = new Vector2(module.OriginX, module.OriginY);
			var tr = new Vector2(module.OriginX + module.Width - 1, module.OriginY);
			var br = new Vector2(module.OriginX + module.Width - 1, module.OriginY + module.Height - 1);
			var bl = new Vector2(module.OriginX, module.OriginY + module.Height - 1);

			var pTl = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)tl.X, (int)tl.Y));
			var pTr = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)tr.X, (int)tr.Y));
			var pBr = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)br.X, (int)br.Y));
			var pBl = _host.Camera.WorldToScreen(IsoMath.GridToWorld((int)bl.X, (int)bl.Y));

			canvas.DrawLine(pTl.X, pTl.Y, pTr.X, pTr.Y);
			canvas.DrawLine(pTr.X, pTr.Y, pBr.X, pBr.Y);
			canvas.DrawLine(pBr.X, pBr.Y, pBl.X, pBl.Y);
			canvas.DrawLine(pBl.X, pBl.Y, pTl.X, pTl.Y);

			// Door positions.
			canvas.StrokeColor = Colors.Orange;
			canvas.StrokeSize = 2;
			foreach (var (dx, dy) in blueprint.EnumerateDoorCells())
			{
				var wx = module.OriginX + dx;
				var wy = module.OriginY + dy;
				var p = _host.Camera.WorldToScreen(IsoMath.GridToWorld(wx, wy));
				canvas.DrawCircle(p.X, p.Y, 6 * _host.Camera.Zoom);
			}
		}
	}
}
