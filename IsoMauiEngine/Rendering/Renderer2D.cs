using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using Microsoft.Maui.Graphics;

namespace IsoMauiEngine.Rendering;

public sealed class Renderer2D
{
	private const float WallPixelHeightBase = 18f;
	private readonly Camera2D _camera;

	public Renderer2D(Camera2D camera)
	{
		_camera = camera;
	}

	public void DrawIsoTile(ICanvas canvas, Vector2 worldPos, float tileW, float tileH)
	{
		var p = _camera.WorldToScreen(worldPos);
		var z = _camera.Zoom;
		tileW *= z;
		tileH *= z;

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
		var z = _camera.Zoom;

		switch (item.Kind)
		{
			case DrawKind.Entity:
				DrawPlayer(canvas, p, item.Facing, item.Frame, item.IsMoving, z, item.IsSuitEquipped);
				break;
			case DrawKind.FloorTile:
					// Default ground tile is sprite-based (deck_plate_normal.png).
					// Keep DrawIsoTile as a fallback/debug renderer if the sprite is missing.
					var deck = SpriteAssets.DeckPlateNormal;
					if (deck is not null)
					{
						DrawGroundTileSprite(canvas, p, deck, z, IsoMath.TileWidth, IsoMath.TileHeight);
					}
					else
					{
						DrawIsoTile(canvas, item.WorldPos, IsoMath.TileWidth, IsoMath.TileHeight);
					}
				break;
			case DrawKind.WallTile:
				DrawIsoWallTile(canvas, item.WorldPos, _camera, item.Height);
				break;
			case DrawKind.DoorTile:
				DrawIsoDoorTile(canvas, item.WorldPos, _camera, item.Height);
				break;
				case DrawKind.Marker:
					DrawMarker(canvas, p, z, Color.FromArgb("#3DE0C4"));
					break;
				case DrawKind.RcsMarker:
					DrawMarker(canvas, p, z, Color.FromArgb("#3DE0C4"));
					break;
				case DrawKind.LockerMarker:
					DrawMarker(canvas, p, z, Color.FromArgb("#B388FF"));
					break;
			default:
				// Backward-compat fallback.
				switch (item.Type)
				{
					case DrawItemType.Player:
						DrawPlayer(canvas, p, item.Facing, item.Frame, item.IsMoving, z, item.IsSuitEquipped);
						break;
					case DrawItemType.Tile:
					default:
						DrawIsoTile(canvas, item.WorldPos, IsoMath.TileWidth, IsoMath.TileHeight);
						break;
				}
				break;
		}
	}

	private static void DrawGroundTileSprite(
		ICanvas canvas,
		Vector2 screenCenter,
		Microsoft.Maui.Graphics.IImage image,
		float zoom,
		float tileW,
		float tileH)
	{
		var w = tileW * zoom;
		var h = tileH * zoom;

		// deck_plate_normal.png is authored as an isometric tile sprite and may include extra transparent
		// padding (often square). To avoid shrinking it too much, scale uniformly based on footprint width.
		// This preserves aspect ratio (no stretching) and keeps the sprite centered at the same point.
		var imgW = MathF.Max(1f, image.Width);
		var imgH = MathF.Max(1f, image.Height);
		var scale = w / imgW;

		var drawW = w;
		var drawH = imgH * scale;
		var x = screenCenter.X - drawW * 0.5f;
		var y = screenCenter.Y - drawH * 0.5f;
		canvas.DrawImage(image, x, y, drawW, drawH);
	}

	private void DrawIsoWallTile(ICanvas canvas, Vector2 worldPos, Camera2D cam, float height)
	{
		DrawIsoCube(canvas, worldPos, cam, height,
			top: Color.FromArgb("#3A454F"),
			right: Color.FromArgb("#2A343D"),
			bottom: Color.FromArgb("#1E262D"),
			panel: null);
	}

	private void DrawIsoDoorTile(ICanvas canvas, Vector2 worldPos, Camera2D cam, float height)
	{
		DrawIsoCube(canvas, worldPos, cam, height,
			top: Color.FromArgb("#46525D"),
			right: Color.FromArgb("#33404A"),
			bottom: Color.FromArgb("#252F37"),
			panel: Color.FromArgb("#1B232A"));
	}

	private void DrawIsoCube(
		ICanvas canvas,
		Vector2 worldPos,
		Camera2D cam,
		float height,
		Color top,
		Color right,
		Color bottom,
		Color? panel)
	{
		var center = cam.WorldToScreen(worldPos);
		var z = cam.Zoom;
		var w2 = (IsoMath.TileWidth * z) * 0.5f;
		var h2 = (IsoMath.TileHeight * z) * 0.5f;

		var wallH = WallPixelHeightBase * z * MathF.Max(0f, height);

		var x = center.X;
		var y = center.Y;

		var t = new Vector2(x, y - h2);
		var r = new Vector2(x + w2, y);
		var b = new Vector2(x, y + h2);
		var l = new Vector2(x - w2, y);

		var down = new Vector2(0, wallH);
		var t2 = t + down;
		var r2 = r + down;
		var b2 = b + down;

		// Bottom (front) face: R->B extruded down.
		var faceBottom = new PathF();
		faceBottom.MoveTo(r.X, r.Y);
		faceBottom.LineTo(b.X, b.Y);
		faceBottom.LineTo(b2.X, b2.Y);
		faceBottom.LineTo(r2.X, r2.Y);
		faceBottom.Close();

		canvas.FillColor = bottom;
		canvas.FillPath(faceBottom);

		// Right face: T->R extruded down.
		var faceRight = new PathF();
		faceRight.MoveTo(t.X, t.Y);
		faceRight.LineTo(r.X, r.Y);
		faceRight.LineTo(r2.X, r2.Y);
		faceRight.LineTo(t2.X, t2.Y);
		faceRight.Close();

		canvas.FillColor = right;
		canvas.FillPath(faceRight);

		// Top face.
		var faceTop = new PathF();
		faceTop.MoveTo(t.X, t.Y);
		faceTop.LineTo(r.X, r.Y);
		faceTop.LineTo(b.X, b.Y);
		faceTop.LineTo(l.X, l.Y);
		faceTop.Close();

		canvas.FillColor = top;
		canvas.FillPath(faceTop);

		// Door styling: inset panel on top face.
		if (panel is not null)
		{
			var iw2 = w2 * 0.55f;
			var ih2 = h2 * 0.55f;
			var it = new Vector2(x, y - ih2);
			var ir = new Vector2(x + iw2, y);
			var ib = new Vector2(x, y + ih2);
			var il = new Vector2(x - iw2, y);
			var inset = new PathF();
			inset.MoveTo(it.X, it.Y);
			inset.LineTo(ir.X, ir.Y);
			inset.LineTo(ib.X, ib.Y);
			inset.LineTo(il.X, il.Y);
			inset.Close();
			canvas.FillColor = panel;
			canvas.FillPath(inset);
		}

		// Subtle outlines.
		canvas.StrokeColor = Color.FromArgb("#0F141A");
		canvas.StrokeSize = 1;
		canvas.DrawPath(faceBottom);
		canvas.DrawPath(faceRight);
		canvas.DrawPath(faceTop);
	}

	private static void DrawMarker(ICanvas canvas, Vector2 screenPos, float zoom, Color color)
	{
		var w2 = (IsoMath.TileWidth * zoom) * 0.2f;
		var h2 = (IsoMath.TileHeight * zoom) * 0.2f;
		var x = screenPos.X;
		var y = screenPos.Y;

		var path = new PathF();
		path.MoveTo(x, y - h2);
		path.LineTo(x + w2, y);
		path.LineTo(x, y + h2);
		path.LineTo(x - w2, y);
		path.Close();

		canvas.FillColor = color;
		canvas.FillPath(path);
		canvas.StrokeColor = Color.FromArgb("#0F141A");
		canvas.StrokeSize = 1;
		canvas.DrawPath(path);
	}

	private static void DrawPlayer(ICanvas canvas, Vector2 screenPos, Direction8 facing, int frame, bool isMoving, float zoom, bool isSuitEquipped)
	{
		var x = (float)screenPos.X;
		var y = (float)screenPos.Y;

		// Prefer sprite sheets if available; fall back to placeholder.
		if (TryDrawPlayerSprite(canvas, x, y, facing, frame, isMoving, zoom, isSuitEquipped))
		{
			return;
		}

		// Body
		canvas.FillColor = Color.FromArgb("#F4D35E");
		var bodyW = 20f * zoom;
		var bodyH = 24f * zoom;
		var radius = 6f * zoom;
		canvas.FillRoundedRectangle(x - bodyW * 0.5f, y - bodyH, bodyW, bodyH, radius);
		canvas.StrokeColor = Colors.Black;
		canvas.StrokeSize = 1;
		canvas.DrawRoundedRectangle(x - bodyW * 0.5f, y - bodyH, bodyW, bodyH, radius);

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
		var headOffset = 20f * zoom;
		var lineLen = 10f * zoom;
		canvas.DrawLine(x, y - headOffset, x + dir.X * lineLen, y - headOffset + dir.Y * lineLen);
	}

	private static bool TryDrawPlayerSprite(ICanvas canvas, float x, float y, Direction8 facing, int frame, bool isMoving, float zoom, bool isSuitEquipped)
	{
		var walking = SpriteAssets.EngineerWalking;
		var suit = SpriteAssets.SpacesuitDirections;

		SpriteSheet? sheet = null;
		int col = 0;
		int row = 0;

		// Spacesuit sheet: 8 direction columns x 1 row, no animation.
		if (isSuitEquipped && suit is not null)
		{
			sheet = suit;
			col = Math.Clamp((int)facing, 0, sheet.Columns - 1);
			row = 0;
		}
		else if (walking is not null)
		{
			// Walking sheet: 8 direction columns x N frame rows.
			// Column order: N, NE, E, SE, S, SW, W, NW (matches Direction8 enum order).
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
			var desiredH = 64f * zoom;
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
