using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using Microsoft.Maui.Graphics;

namespace IsoMauiEngine.Rendering;

public sealed class Renderer2D
{
	private readonly Camera2D _camera;

	public Renderer2D(Camera2D camera)
	{
		_camera = camera;
	}

	public void DrawIsoTile(ICanvas canvas, Vector2 worldPos, float tileW, float tileH)
	{
		var p = _camera.WorldToScreen(worldPos);

		var x = (float)p.X;
		var y = (float)p.Y;

		var path = new PathF();
		path.MoveTo(x, y - tileH / 2f);
		path.LineTo(x + tileW / 2f, y);
		path.LineTo(x, y + tileH / 2f);
		path.LineTo(x - tileW / 2f, y);
		path.Close();

		canvas.FillColor = Color.FromArgb("#1F2A33");
		canvas.FillPath(path);
		canvas.StrokeColor = Color.FromArgb("#2F3D49");
		canvas.StrokeSize = 1;
		canvas.DrawPath(path);
	}

	public void DrawSpriteOrPlaceholder(ICanvas canvas, DrawItem item)
	{
		var p = _camera.WorldToScreen(item.WorldPos);

		switch (item.Type)
		{
			case DrawItemType.Player:
				DrawPlayer(canvas, p, item.Facing, item.Frame, item.IsMoving);
				break;
			case DrawItemType.Tile:
			default:
				DrawIsoTile(canvas, item.WorldPos, IsoMath.TileWidth, IsoMath.TileHeight);
				break;
		}
	}

	private static void DrawPlayer(ICanvas canvas, Vector2 screenPos, Direction8 facing, int frame, bool isMoving)
	{
		var x = (float)screenPos.X;
		var y = (float)screenPos.Y;

		// Prefer sprite sheets if available; fall back to placeholder.
		if (TryDrawPlayerSprite(canvas, x, y, facing, frame, isMoving))
		{
			return;
		}

		// Body
		canvas.FillColor = Color.FromArgb("#F4D35E");
		canvas.FillRoundedRectangle(x - 10, y - 24, 20, 24, 6);
		canvas.StrokeColor = Colors.Black;
		canvas.StrokeSize = 1;
		canvas.DrawRoundedRectangle(x - 10, y - 24, 20, 24, 6);

		// Facing indicator
		var dir = facing switch
		{
			Direction8.N => new Vector2(0, -1),
			Direction8.NE => Vector2.Normalize(new Vector2(1, -1)),
			Direction8.E => new Vector2(1, 0),
			Direction8.SE => Vector2.Normalize(new Vector2(1, 1)),
			Direction8.S => new Vector2(0, 1),
			Direction8.SW => Vector2.Normalize(new Vector2(-1, 1)),
			Direction8.W => new Vector2(-1, 0),
			Direction8.NW => Vector2.Normalize(new Vector2(-1, -1)),
			_ => new Vector2(0, 1),
		};

		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 2;
		canvas.DrawLine(x, y - 20, x + dir.X * 10, y - 20 + dir.Y * 10);
	}

	private static bool TryDrawPlayerSprite(ICanvas canvas, float x, float y, Direction8 facing, int frame, bool isMoving)
	{
		var walking = SpriteAssets.EngineerWalking;

		SpriteSheet? sheet = null;
		int col = 0;
		int row = 0;

		// Walking sheet: 8 direction columns x 4 frame rows.
		// Column order: N, NE, E, SE, S, SW, W, NW (matches Direction8 enum order).
		// Row order: animation frames 0..3.
		if (walking is not null)
		{
			sheet = walking;
			col = Math.Clamp((int)facing, 0, sheet.Columns - 1);
			row = isMoving
				? (((frame % sheet.Rows) + sheet.Rows) % sheet.Rows)
				: 0;
		}

		if (sheet is null)
		{
			return false;
		}

		try
		{
			var src = sheet.GetSourceRect(col, row);

			// Draw with the feet anchored at the world position.
			var desiredH = 64f;
			var aspect = src.Width / MathF.Max(1f, src.Height);
			var desiredW = desiredH * aspect;
			var dest = new RectF(x - desiredW * 0.5f, y - desiredH, desiredW, desiredH);

			// Crop by clipping the destination rect and drawing the full sheet scaled/translated.
			var scaleX = dest.Width / MathF.Max(1f, src.Width);
			var scaleY = dest.Height / MathF.Max(1f, src.Height);
			var drawX = dest.X - src.X * scaleX;
			var drawY = dest.Y - src.Y * scaleY;
			var drawW = (float)sheet.Image.Width * scaleX;
			var drawH = (float)sheet.Image.Height * scaleY;

			canvas.SaveState();
			canvas.ClipRectangle(dest);
			canvas.DrawImage(sheet.Image, drawX, drawY, drawW, drawH);
			canvas.RestoreState();
			return true;
		}
		catch
		{
			return false;
		}
	}


}
