using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Navigation;
using IsoMauiEngine.World.Modules;
using Microsoft.Maui.Graphics;

namespace IsoMauiEngine.Rendering;

public sealed class IsoDrawable : IDrawable
{
	private readonly GameHost _host;
	private readonly List<DrawItem> _allItems = new(capacity: 2048);
	private readonly List<DrawItem> _tileItems = new(capacity: 2048);
	private readonly List<DrawItem> _doorItems = new(capacity: 256);
	private readonly List<DrawItem> _wallItems = new(capacity: 512);
	private readonly List<DrawItem> _lockerItems = new(capacity: 64);
	private readonly List<DrawItem> _rcsItems = new(capacity: 64);
	private readonly List<DrawItem> _entityItems = new(capacity: 512);
	private readonly List<DrawItem> _occluderItems = new(capacity: 2048);

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
		_doorItems.Clear();
		_wallItems.Clear();
		_lockerItems.Clear();
		_rcsItems.Clear();
		_entityItems.Clear();
		_occluderItems.Clear();

		_host.World.AppendDrawItems(_allItems);
		for (var i = 0; i < _allItems.Count; i++)
		{
			var item = _allItems[i];
			if (item.Type == DrawItemType.Tile)
			{
				if (item.Kind == DrawKind.DoorTile)
				{
					_doorItems.Add(item);
				}
				else if (item.Kind == DrawKind.WallTile)
				{
					_wallItems.Add(item);
				}
				else if (item.Kind == DrawKind.LockerMarker)
				{
					_lockerItems.Add(item);
				}
				else if (item.Kind == DrawKind.RcsMarker)
				{
					_rcsItems.Add(item);
				}
				else
				{
					_tileItems.Add(item);
				}
			}
			else
			{
				_entityItems.Add(item);
			}
		}

		// Ground pass: tiles never occlude entities.
		// Draw a floor base under doors first, then draw remaining floor/markers.
		for (var i = 0; i < _doorItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _doorItems[i] with
			{
				Kind = DrawKind.FloorTile,
				Height = 0f,
				LayerBias = -1000f,
			});
		}

		_tileItems.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
		for (var i = 0; i < _tileItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _tileItems[i]);
		}

		// Dynamic occlusion pass:
		// - Doors/walls north, north-east, north-west, and west of the player render before the player.
		// - Doors/walls south, south-east, south-west, and east of the player render after the player.
		// - Walls directly north or west of a door (i.e., the wall tile has a door immediately south or east)
		//   are biased to render behind that door.
		var playerPos = _host.World.Player.WorldPos;
		var playerItem = default(DrawItem);
		for (var i = 0; i < _entityItems.Count; i++)
		{
			if (_entityItems[i].Type == DrawItemType.Player)
			{
				playerItem = _entityItems[i];
				break;
			}
		}
		var playerCell = GridCellFromWorld(playerPos);
		var wallBiasByCell = new Dictionary<(int moduleId, int x, int y), float>(capacity: _wallItems.Count);

		for (var i = 0; i < _wallItems.Count; i++)
		{
			var wall = _wallItems[i];
			var occlusionBias = GetPlayerOcclusionBias(wall.WorldPos, playerPos);
			var doorAdjBias = IsWallNorthOrWestOfDoor(wall) ? -0.05f : +0.05f;
			var computedBias = wall.LayerBias + occlusionBias + doorAdjBias;
			_occluderItems.Add(wall with { LayerBias = computedBias });
			if (_host.World.TryFindContainingModule(wall.WorldPos, out var wm, out var wc))
			{
				wallBiasByCell[(wm.ModuleId, wc.X, wc.Y)] = computedBias;
			}
		}

		for (var i = 0; i < _doorItems.Count; i++)
		{
			var door = ResolveDoorFrame(_doorItems[i], out var isWalkableDoorTile);
			var occlusionBias = GetPlayerOcclusionBias(door.WorldPos, playerPos);

			// Special-case: if the player is standing on (or rounding onto) a walkable door tile,
			// draw the door before the player so the player overlays the doorway.
			// This also covers the "entering from the south" transition where grid rounding may
			// already snap the player onto the door tile mid-step.
			if (isWalkableDoorTile && GridCellFromWorld(door.WorldPos) == playerCell)
			{
				occlusionBias = -100_000f;
			}
			_occluderItems.Add(door with { LayerBias = door.LayerBias + occlusionBias });
		}

		// Locker props: draw after walls/doors but before the player.
		for (var i = 0; i < _lockerItems.Count; i++)
		{
			var locker = _lockerItems[i];
			// Default: keep lockers between walls (typically +/-100k) and the player (near 0).
			var computedBias = locker.LayerBias - 90_000f;

			// If the locker has any surrounding wall tiles (8-neighborhood), render the locker
			// after those wall tiles by pushing it just past the maximum adjacent wall bias.
			if (_host.World.TryFindContainingModule(locker.WorldPos, out var lm, out var lc))
			{
				var maxAdjWallBias = float.NegativeInfinity;
				var hasAdjWall = false;
				for (var dy = -1; dy <= 1; dy++)
				{
					for (var dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0)
						{
							continue;
						}
						var nx = lc.X + dx;
						var ny = lc.Y + dy;
						var ncell = new AStarGrid.Cell(nx, ny);
						if (_host.World.TryGetCellKind(lm, ncell, out var nk) && nk == CellKind.Wall)
						{
							hasAdjWall = true;
							if (wallBiasByCell.TryGetValue((lm.ModuleId, nx, ny), out var wb))
							{
								maxAdjWallBias = MathF.Max(maxAdjWallBias, wb);
							}
							else
							{
								// Fallback if we didn't cache the wall bias for some reason.
								var wWorld = IsoMath.GridToWorld(nx, ny) + lm.WorldOffset;
								var wb2 = locker.LayerBias + GetPlayerOcclusionBias(wWorld, playerPos) + 0.05f;
								maxAdjWallBias = MathF.Max(maxAdjWallBias, wb2);
							}
						}
					}
				}

				if (hasAdjWall && float.IsFinite(maxAdjWallBias))
				{
					computedBias = MathF.Max(computedBias, maxAdjWallBias + 0.01f);
				}
			}

			_occluderItems.Add(locker with { LayerBias = computedBias });
		}

		// RCS console prop: same layering rules as locker.
		for (var i = 0; i < _rcsItems.Count; i++)
		{
			var rcs = _rcsItems[i];
			// RCS must render before the player and before lockers.
			// Lockers default to -90_000f, so keep RCS strictly more negative.
			const float rcsUpperBound = -90_001f;
			var computedBias = rcs.LayerBias - 95_000f;

			if (_host.World.TryFindContainingModule(rcs.WorldPos, out var rm, out var rc))
			{
				var maxAdjWallBias = float.NegativeInfinity;
				var hasAdjWall = false;
				for (var dy = -1; dy <= 1; dy++)
				{
					for (var dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0)
						{
							continue;
						}
						var nx = rc.X + dx;
						var ny = rc.Y + dy;
						var ncell = new AStarGrid.Cell(nx, ny);
						if (_host.World.TryGetCellKind(rm, ncell, out var nk) && nk == CellKind.Wall)
						{
							hasAdjWall = true;
							if (wallBiasByCell.TryGetValue((rm.ModuleId, nx, ny), out var wb))
							{
								maxAdjWallBias = MathF.Max(maxAdjWallBias, wb);
							}
							else
							{
								var wWorld = IsoMath.GridToWorld(nx, ny) + rm.WorldOffset;
								var wb2 = rcs.LayerBias + GetPlayerOcclusionBias(wWorld, playerPos) + 0.05f;
								maxAdjWallBias = MathF.Max(maxAdjWallBias, wb2);
							}
						}
					}
				}

				if (hasAdjWall && float.IsFinite(maxAdjWallBias))
				{
					// Try to draw after the adjacent wall, but never exceed the upper bound
					// (which would place the console after lockers or potentially after the player).
					var required = maxAdjWallBias + 0.01f;
					computedBias = MathF.Max(computedBias, required);
					computedBias = MathF.Min(computedBias, rcsUpperBound);
				}
			}

			_occluderItems.Add(rcs with { LayerBias = computedBias });
		}

		for (var i = 0; i < _entityItems.Count; i++)
		{
			_occluderItems.Add(_entityItems[i]);
		}

		_occluderItems.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
		for (var i = 0; i < _occluderItems.Count; i++)
		{
			_host.Renderer.DrawSpriteOrPlaceholder(canvas, _occluderItems[i]);
		}

		DrawEvaHudIndicators(canvas, dirtyRect);

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

	private static float GetPlayerOcclusionBias(Vector2 itemWorldPos, Vector2 playerWorldPos)
	{
		// Use grid directions for stable classification across camera zoom/pan.
		var itemGrid = IsoMath.WorldToGrid(itemWorldPos);
		var playerGrid = IsoMath.WorldToGrid(playerWorldPos);
		var ix = (int)MathF.Round(itemGrid.X);
		var iy = (int)MathF.Round(itemGrid.Y);
		var px = (int)MathF.Round(playerGrid.X);
		var py = (int)MathF.Round(playerGrid.Y);
		var dx = ix - px;
		var dy = iy - py;

		// Behind player: north, north-east, north-west, and west.
		// In front of player: south, south-east, south-west, and east.
		var isBehindPlayer = dy < 0 || (dy == 0 && dx < 0);
		return isBehindPlayer ? -100_000f : 100_000f;
	}

	private bool IsWallNorthOrWestOfDoor(DrawItem wallItem)
	{
		// "Walls in tiles next to a door to the north and west of the door" ==
		// wall tile whose south neighbor is a door OR whose east neighbor is a door.
		if (!_host.World.TryFindContainingModule(wallItem.WorldPos, out var module, out var cell))
		{
			return false;
		}
		if (!_host.World.TryGetCellKind(module, cell, out var kind) || kind != CellKind.Wall)
		{
			return false;
		}

		var minX = module.OriginX;
		var minY = module.OriginY;
		var maxX = module.OriginX + module.Width - 1;
		var maxY = module.OriginY + module.Height - 1;

		// South neighbor (door below) => wall is north of a door.
		if (cell.Y < maxY)
		{
			var south = new AStarGrid.Cell(cell.X, cell.Y + 1);
			if (south.X >= minX && south.X <= maxX && south.Y >= minY && south.Y <= maxY
				&& _host.World.TryGetCellKind(module, south, out var southKind)
				&& southKind == CellKind.Door)
			{
				return true;
			}
		}

		// East neighbor (door right) => wall is west of a door.
		if (cell.X < maxX)
		{
			var east = new AStarGrid.Cell(cell.X + 1, cell.Y);
			if (east.X >= minX && east.X <= maxX && east.Y >= minY && east.Y <= maxY
				&& _host.World.TryGetCellKind(module, east, out var eastKind)
				&& eastKind == CellKind.Door)
			{
				return true;
			}
		}

		return false;
	}

	private static (int X, int Y) GridCellFromWorld(Vector2 world)
	{
		var g = IsoMath.WorldToGrid(world);
		return ((int)MathF.Round(g.X), (int)MathF.Round(g.Y));
	}

	private DrawItem ResolveDoorFrame(DrawItem item, out bool isWalkableDoorTile)
	{
		// Door0: N/S open, Door1: N/S closed, Door2: E/W open, Door3: E/W closed.
		if (_host.World.TryFindContainingModule(item.WorldPos, out var module, out var cell)
			&& _host.World.TryGetCellKind(module, cell, out var kind)
			&& kind == CellKind.Door
			&& module.TryGetDoorSideAtWorldCell(cell.X, cell.Y, out var side))
		{
			var isOpen = _host.World.IsWalkableCellInModule(module, cell.X, cell.Y);
			isWalkableDoorTile = isOpen;
			var isNorthSouth = side is DoorSide.North or DoorSide.South;
			var frame = isNorthSouth
				? (isOpen ? 0 : 1)
				: (isOpen ? 2 : 3);
			return item with { Frame = frame };
		}

		isWalkableDoorTile = false;
		return item;
	}

	private void DrawEvaHudIndicators(ICanvas canvas, RectF viewport)
	{
		// Suit-gated EVA indicators.
		// Also show while RCS mode is active (player is "seated"/locked to console inside a module).
		var rcsActive = _host.World.RcsModeModule is not null;
		if (!_host.World.Player.IsSuitEquipped && !rcsActive)
		{
			return;
		}
		if (!rcsActive && _host.World.TryFindContainingModule(_host.World.Player.WorldPos, out _, out _))
		{
			return;
		}

		var playerWorld = _host.World.Player.WorldPos;
		var playerScreen = _host.Camera.WorldToScreen(playerWorld);
		var margin = 26f;
		var left = viewport.X + margin;
		var right = viewport.X + viewport.Width - margin;
		var top = viewport.Y + margin;
		var bottom = viewport.Y + viewport.Height - margin;

		ShipModuleInstance? home = null;
		for (var i = 0; i < _host.World.Modules.Count; i++)
		{
			var m = _host.World.Modules[i];
			if (m.IsCommandModule)
			{
				home = m;
				break;
			}
		}

		// Build a list of candidate derelict modules (nearest N).
		var candidates = new List<(ShipModuleInstance m, float d2)>(capacity: 16);
		for (var i = 0; i < _host.World.Modules.Count; i++)
		{
			var m = _host.World.Modules[i];
			if (!m.IsDerelict)
			{
				continue;
			}
			var d2 = Vector2.DistanceSquared(playerWorld, m.GetWorldCenter());
			candidates.Add((m, d2));
		}
		candidates.Sort(static (a, b) => a.d2.CompareTo(b.d2));
		var max = Math.Min(6, candidates.Count);

		// HOME indicator (single, distinct).
		if (home is not null)
		{
			var dist = Vector2.Distance(playerWorld, home.GetWorldCenter());
			DrawEdgeArrow(canvas, playerScreen, _host.Camera.WorldToScreen(home.GetWorldCenter()), left, right, top, bottom,
				color: Color.FromArgb("#06D6A0"), label: $"HOME {dist:0}");
		}

		for (var i = 0; i < max; i++)
		{
			var m = candidates[i].m;
			var dist = Vector2.Distance(playerWorld, m.GetWorldCenter());
			DrawEdgeArrow(canvas, playerScreen, _host.Camera.WorldToScreen(m.GetWorldCenter()), left, right, top, bottom,
				color: Color.FromArgb("#FFD166"), label: $"#{m.ModuleId} {dist:0}");
		}
	}

	private static void DrawEdgeArrow(
		ICanvas canvas,
		PointF from,
		PointF to,
		float left,
		float right,
		float top,
		float bottom,
		Color color,
		string label)
	{
		var vx = (float)(to.X - from.X);
		var vy = (float)(to.Y - from.Y);
		var len = MathF.Sqrt(vx * vx + vy * vy);
		if (len < 1e-3f)
		{
			return;
		}
		vx /= len;
		vy /= len;

		float tx = float.PositiveInfinity;
		if (MathF.Abs(vx) > 1e-5f)
		{
			tx = vx > 0 ? (right - from.X) / vx : (left - from.X) / vx;
		}
		float ty = float.PositiveInfinity;
		if (MathF.Abs(vy) > 1e-5f)
		{
			ty = vy > 0 ? (bottom - from.Y) / vy : (top - from.Y) / vy;
		}
		var t = MathF.Min(tx, ty);
		if (!float.IsFinite(t) || t <= 0f)
		{
			return;
		}

		var px = from.X + vx * t;
		var py = from.Y + vy * t;

		// Arrow triangle.
		var tip = new PointF(px, py);
		var back = new PointF(px - vx * 14f, py - vy * 14f);
		var perpX = -vy;
		var perpY = vx;
		var leftPt = new PointF(back.X + perpX * 7f, back.Y + perpY * 7f);
		var rightPt = new PointF(back.X - perpX * 7f, back.Y - perpY * 7f);

		var path = new PathF();
		path.MoveTo(tip.X, tip.Y);
		path.LineTo(leftPt.X, leftPt.Y);
		path.LineTo(rightPt.X, rightPt.Y);
		path.Close();

		canvas.FillColor = color;
		canvas.FillPath(path);
		canvas.StrokeColor = Color.FromArgb("#0B0F14");
		canvas.StrokeSize = 1;
		canvas.DrawPath(path);

		canvas.FontColor = color;
		canvas.FontSize = 12;
		canvas.DrawString(label, tip.X - 48, tip.Y - 22, 96, 18, HorizontalAlignment.Center, VerticalAlignment.Center);
	}

	private void DrawNavigationDebug(ICanvas canvas)
	{
		var nav = _host.Navigation;
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12;
		canvas.DrawString($"Mode: {nav.CurrentMode}", 8, 26, HorizontalAlignment.Left);
		canvas.DrawString($"Navigator: {nav.ActiveNavigator}", 8, 42, HorizontalAlignment.Left);

		var linksCount = _host.World.ModuleGraph.EnumerateUniqueLinksSnapshot().Count;
		canvas.DrawString($"Links: {linksCount}  GraphVersion: {_host.World.ModuleGraph.Version}", 8, 58, HorizontalAlignment.Left);

		var suitText = _host.World.Player.IsSuitEquipped ? "Suit: ON" : "Suit: OFF";
		canvas.DrawString(suitText, 8, 74, HorizontalAlignment.Left);

		var homeText = "Home: (none)";
		for (var i = 0; i < _host.World.Modules.Count; i++)
		{
			if (_host.World.Modules[i].IsCommandModule)
			{
				homeText = $"Home: #{_host.World.Modules[i].ModuleId}";
				break;
			}
		}
		canvas.DrawString(homeText, 8, 90, HorizontalAlignment.Left);

		var pendingText = nav.Pending.HasValue
			? $"PendingInteract: {nav.Pending.Value.Kind} (Module #{nav.Pending.Value.ModuleId})"
			: "PendingInteract: (none)";
		canvas.DrawString(pendingText, 8, 106, HorizontalAlignment.Left);

		var lineY = 122f;
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
			var hasAnyLinkedDoor = false;
			foreach (DoorSide probeSide in Enum.GetValues(typeof(DoorSide)))
			{
				if (graph.TryGetLink(module.ModuleId, probeSide, out _))
				{
					hasAnyLinkedDoor = true;
					break;
				}
			}

			foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
			{
				var linked = graph.TryGetLink(module.ModuleId, side, out _);
				var passable = module.IsAirlock || !hasAnyLinkedDoor || linked;

				// Colors represent passability (not just link state):
				// - Linked passable door: green
				// - Airlock unlinked but passable: blue
				// - Floating module (no links) passable doors: amber
				// - Connected module unlinked blocked doors: red
				canvas.StrokeColor = passable
					? (linked
						? Color.FromArgb("#06D6A0")
						: (module.IsAirlock ? Color.FromArgb("#4D96FF") : Color.FromArgb("#FFD166")))
					: Color.FromArgb("#EF476F");
				var p = _host.Camera.WorldToScreen(module.GetDoorWorldPos(side));
				canvas.DrawCircle(p.X, p.Y, 6 * _host.Camera.Zoom);
			}
		}
	}
}
