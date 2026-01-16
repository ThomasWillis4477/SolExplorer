using System.Numerics;
using IsoMauiEngine.Engine;
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
		_tileItems.Sort(static (a, b) => a.SortY.CompareTo(b.SortY));
		for (var i = 0; i < _tileItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _tileItems[i]);
		}

		// Entity pass: depth sort entities by their feet (SortY).
		_entityItems.Sort(static (a, b) => a.SortY.CompareTo(b.SortY));
		for (var i = 0; i < _entityItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _entityItems[i]);
		}

		// Simple debug HUD
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12;
		canvas.DrawString($"Items: {_allItems.Count}", 8, 8, HorizontalAlignment.Left);
	}
}
