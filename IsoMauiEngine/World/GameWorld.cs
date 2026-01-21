using System.Diagnostics;
using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Entities;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Navigation;
using IsoMauiEngine.Rendering;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.World;

public sealed class GameWorld
{
	private const float DoorLinkBreakDistance = 28f;
	private const float LinkValidationIntervalSeconds = 0.25f;
	private float _linkValidationTimer;

	private readonly List<Entity> _entities = new();
	private readonly List<ShipModuleInstance> _modules = new();
	private readonly List<ModuleGridMap> _moduleMaps = new();
	private readonly Vector2 _spawnWorldPos;
	private readonly List<CircleObstacle> _spaceObstacles = new();

	public GameWorld()
	{
		ModuleGraph = new ModuleGraph();
		BuildScene();
		_spawnWorldPos = ComputeSpawnAfterPlacement();
		
		Player = new Player
		{
			WorldPos = _spawnWorldPos
		};

		_entities.Add(Player);
	}

	/// <summary>
	/// All placed modules in the scene (order is placement order).
	/// </summary>
	public IReadOnlyList<ShipModuleInstance> Modules => _modules;
	public ModuleGraph ModuleGraph { get; }
	public IReadOnlyList<CircleObstacle> SpaceObstacles => _spaceObstacles;
	public ShipModuleInstance? RcsModeModule { get; set; }

	/// <summary>
	/// Back-compat: first module (the "first generated room").
	/// </summary>
	public ShipModuleInstance Module => _modules[0];

	public Player Player { get; }

	public Func<Vector2, bool> CanMoveToWorld => CanPlayerMoveToWorld;

	public void Update(float dt, InputState input)
	{
		// If RCS mode is active, lock the player onto the RCS console tile of the active module
		// so the player rides along as the module moves (WorldOffset changes).
		if (RcsModeModule is not null)
		{
			var m = RcsModeModule;
			var rcsWorld = GetRcsConsoleWorldPos(m);
			Player.WorldPos = rcsWorld;
			Player.SetMotion(Vector2.Zero, isMoving: false);
		}

		_linkValidationTimer += dt;
		if (_linkValidationTimer >= LinkValidationIntervalSeconds)
		{
			_linkValidationTimer = 0f;
			ValidateDoorLinks();
		}

		for (var i = 0; i < _entities.Count; i++)
		{
			_entities[i].Update(dt, input);
		}
	}

	private static Vector2 GetRcsConsoleWorldPos(ShipModuleInstance module)
	{
		var local = module.Blueprint.RcsControl;
		var worldGridX = module.OriginX + (int)local.X;
		var worldGridY = module.OriginY + (int)local.Y;
		return IsoMath.GridToWorld(worldGridX, worldGridY) + module.WorldOffset;
	}

	public void AppendDrawItems(List<DrawItem> drawItems)
	{
		for (var i = 0; i < _moduleMaps.Count; i++)
		{
			_moduleMaps[i].AppendDrawItems(drawItems);
		}

		drawItems.Add(Player.CreateDrawItem());

		for (var i = 0; i < _entities.Count; i++)
		{
			if (ReferenceEquals(_entities[i], Player))
			{
				continue;
			}
			_entities[i].EmitDrawItems(drawItems);
		}
	}

	private bool CanPlayerMoveToWorld(Vector2 nextWorld)
	{
		var curWorld = Player.WorldPos;
		var curInside = TryFindContainingModule(curWorld, out var curModule, out var curCell);
		var nextInside = TryFindContainingModule(nextWorld, out var nextModule, out var nextCell);

		// Space -> space: always allowed.
		if (!curInside && !nextInside)
		{
			return true;
		}

		// Space -> module: only allow entering via a door tile.
		if (!curInside && nextInside)
		{
			return TryGetCellKind(nextModule, nextCell, out var k)
				&& k == CellKind.Door
				&& IsDoorTilePassable(nextModule, nextCell.X, nextCell.Y);
		}

		// Module -> space: only allow exiting via a door tile.
		if (curInside && !nextInside)
		{
			return TryGetCellKind(curModule, curCell, out var k)
				&& k == CellKind.Door
				&& IsDoorTilePassable(curModule, curCell.X, curCell.Y);
		}

		// Module -> module: enforce walkability on the destination cell.
		if (nextInside)
		{
			return IsWalkableCellInModule(nextModule, nextCell.X, nextCell.Y);
		}

		return true;
	}

	public bool IsWalkableCellInModule(ShipModuleInstance module, int worldGridX, int worldGridY)
	{
		for (var i = 0; i < _moduleMaps.Count; i++)
		{
			if (_moduleMaps[i].Module.ModuleId == module.ModuleId)
			{
				if (_moduleMaps[i].TryGetCellKind(worldGridX, worldGridY, out var kind)
					&& kind == CellKind.Door
					&& !IsDoorTilePassable(module, worldGridX, worldGridY))
				{
					return false;
				}
				return _moduleMaps[i].IsWalkableCell(worldGridX, worldGridY);
			}
		}
		return false;
	}

	private bool IsDoorTilePassable(ShipModuleInstance module, int worldGridX, int worldGridY)
	{
		if (module.IsAirlock)
		{
			return true;
		}
		if (!module.TryGetDoorSideAtWorldCell(worldGridX, worldGridY, out var side))
		{
			// If we can't map it to a side, fall back to blueprint walkability.
			return true;
		}

		// Door passability rules:
		// - Airlocks: always passable (handled above)
		// - If a module has zero linked doors, treat all its doors as passable ("floating" module)
		// - Otherwise, only the linked doors are passable
		var hasAnyLinkedDoor = false;
		var sides = new[] { DoorSide.North, DoorSide.South, DoorSide.East, DoorSide.West };
		for (var i = 0; i < sides.Length; i++)
		{
			if (ModuleGraph.TryGetLink(module.ModuleId, sides[i], out _))
			{
				hasAnyLinkedDoor = true;
				break;
			}
		}

		if (!hasAnyLinkedDoor)
		{
			return true;
		}

		return ModuleGraph.TryGetLink(module.ModuleId, side, out _);
	}

	public bool TryGetCellKind(ShipModuleInstance module, AStarGrid.Cell cell, out CellKind kind)
	{
		for (var i = 0; i < _moduleMaps.Count; i++)
		{
			if (_moduleMaps[i].Module.ModuleId == module.ModuleId)
			{
				return _moduleMaps[i].TryGetCellKind(cell.X, cell.Y, out kind);
			}
		}
		kind = default;
		return false;
	}

	public bool TryFindContainingModule(Vector2 worldPos, out ShipModuleInstance module, out AStarGrid.Cell cell)
	{
		for (var i = 0; i < _modules.Count; i++)
		{
			var m = _modules[i];
			var g = IsoMath.WorldToGrid(worldPos - m.WorldOffset);
			var gx = (int)MathF.Round(g.X);
			var gy = (int)MathF.Round(g.Y);

			if (gx >= m.OriginX && gx < m.OriginX + m.Width && gy >= m.OriginY && gy < m.OriginY + m.Height)
			{
				module = m;
				cell = new AStarGrid.Cell(gx, gy);
				return true;
			}
		}
		module = null!;
		cell = default;
		return false;
	}

	public ShipModuleInstance? GetModuleById(int id)
	{
		for (var i = 0; i < _modules.Count; i++)
		{
			if (_modules[i].ModuleId == id)
			{
				return _modules[i];
			}
		}
		return null;
	}

	private void BuildScene()
	{
		_modules.Clear();
		_moduleMaps.Clear();
		_spaceObstacles.Clear();

		// Deterministic "salvage demo" scene:
		// - Starter ship near origin with linked modules
		// - Wreck far away that requires EVA
		// - Detached wreck modules nearby
		// - Debris obstacles between clusters
		// No rotation anywhere; placement uses WorldOffset only.

		ShipModuleInstance AddModule(ModuleSizePreset preset, Vector2 worldOffset, bool isDerelict, Action<ModuleBlueprint>? configureBlueprint = null)
		{
			var bp = BlueprintLibrary.Get(preset);
			configureBlueprint?.Invoke(bp);
			var m = new ShipModuleInstance(bp, preset, originX: 0, originY: 0)
			{
				WorldOffset = worldOffset,
				IsDerelict = isDerelict
			};
			_modules.Add(m);
			_moduleMaps.Add(new ModuleGridMap(m));
			return m;
		}

		static bool AreOpposite(DoorSide a, DoorSide b)
		{
			return (a == DoorSide.North && b == DoorSide.South)
				|| (a == DoorSide.South && b == DoorSide.North)
				|| (a == DoorSide.East && b == DoorSide.West)
				|| (a == DoorSide.West && b == DoorSide.East);
		}

		static (float minX, float minY, float maxX, float maxY) GetWorldAabb(ShipModuleInstance m)
		{
			// Conservative world AABB from the 4 grid corners.
			var tl = IsoMath.GridToWorld(m.OriginX, m.OriginY) + m.WorldOffset;
			var tr = IsoMath.GridToWorld(m.OriginX + m.Width - 1, m.OriginY) + m.WorldOffset;
			var br = IsoMath.GridToWorld(m.OriginX + m.Width - 1, m.OriginY + m.Height - 1) + m.WorldOffset;
			var bl = IsoMath.GridToWorld(m.OriginX, m.OriginY + m.Height - 1) + m.WorldOffset;

			var minX = MathF.Min(MathF.Min(tl.X, tr.X), MathF.Min(br.X, bl.X));
			var maxX = MathF.Max(MathF.Max(tl.X, tr.X), MathF.Max(br.X, bl.X));
			var minY = MathF.Min(MathF.Min(tl.Y, tr.Y), MathF.Min(br.Y, bl.Y));
			var maxY = MathF.Max(MathF.Max(tl.Y, tr.Y), MathF.Max(br.Y, bl.Y));
			return (minX, minY, maxX, maxY);
		}

		static bool AabbIntersects((float minX, float minY, float maxX, float maxY) a, (float minX, float minY, float maxX, float maxY) b)
		{
			return a.minX < b.maxX && a.maxX > b.minX && a.minY < b.maxY && a.maxY > b.minY;
		}

		void AlignAndPlaceForDoorLink(ShipModuleInstance a, DoorSide sideA, ShipModuleInstance b, DoorSide sideB, float maxAlignDistance)
		{
			// Doors must be adjacent, not coincident:
			// doorB == doorA + seamStep, where seamStep is the outward one-cell step from module A.
			if (!AreOpposite(sideA, sideB))
			{
				Debug.WriteLine($"[BuildScene] Refusing to align non-opposite doors: A#{a.ModuleId}.{sideA} <-> B#{b.ModuleId}.{sideB}");
				return;
			}

			var aDoor = a.GetDoorWorldPos(sideA);
			var seamStep = ShipModuleInstance.GetWorldStepForSide(sideA);
			var desiredBDoor = aDoor + seamStep;

			var bDoorCell = b.GetDoorWorldCell(sideB);
			b.WorldOffset = desiredBDoor - IsoMath.GridToWorld(bDoorCell.x, bDoorCell.y);

			var bDoor2 = b.GetDoorWorldPos(sideB);
			if (Vector2.Distance(desiredBDoor, bDoor2) <= maxAlignDistance)
			{
				ModuleGraph.TryLinkDoors(a.ModuleId, sideA, b.ModuleId, sideB);
			}
			else
			{
				Debug.WriteLine($"[BuildScene] Door align miss: A#{a.ModuleId}.{sideA} -> B#{b.ModuleId}.{sideB} dist={Vector2.Distance(desiredBDoor, bDoor2):F3}");
			}
		}

		// --- Starter ship (near origin) ---
		var starterOrigin = Vector2.Zero;
		var starterCommand = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false);
		starterCommand.IsCommandModule = true;
		var starterCargo = AddModule(ModuleSizePreset.Large, starterOrigin, isDerelict: false);
		var starterAirlock = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false, configureBlueprint: bp =>
		{
			// Locker equipment tile inside the starter airlock module.
			bp.Locker = new Vector2(3, 3);
		});
		starterAirlock.IsAirlock = true;
		var starterGenerator = AddModule(ModuleSizePreset.Medium, starterOrigin, isDerelict: false);
		var starterLifeSupport = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false);
		var starterEngine = AddModule(ModuleSizePreset.Medium, starterOrigin, isDerelict: false);

		// Link/align modules (example graph):
		// Command East <-> Cargo West
		// Command South <-> Airlock North
		// Cargo South <-> Generator North
		// Cargo East <-> LifeSupport West
		// Command West <-> Engine East (optional)
		AlignAndPlaceForDoorLink(starterCommand, DoorSide.West, starterCargo, DoorSide.East, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(starterCargo, DoorSide.South, starterAirlock, DoorSide.North, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(starterCargo, DoorSide.North, starterLifeSupport, DoorSide.South, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(starterCargo, DoorSide.West, starterGenerator, DoorSide.East, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(starterGenerator, DoorSide.West, starterEngine, DoorSide.East, maxAlignDistance: 1f);

		// --- Wreck (far away) ---
		var wreckBase = new Vector2(2600f, 1400f);
		var wreckCommand = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);
		var wreckCargo = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);
		var wreckGen = AddModule(ModuleSizePreset.Small, wreckBase, isDerelict: true);
		var wreckLife = AddModule(ModuleSizePreset.Small, wreckBase, isDerelict: true);
		var wreckEngine = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);

		// Partial linked cluster in the wreck.
		AlignAndPlaceForDoorLink(wreckCommand, DoorSide.East, wreckCargo, DoorSide.West, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(wreckCargo, DoorSide.South, wreckGen, DoorSide.North, maxAlignDistance: 1f);
		AlignAndPlaceForDoorLink(wreckCommand, DoorSide.West, wreckEngine, DoorSide.East, maxAlignDistance: 1f);
		// Leave wreckLife detached but nearby.
		wreckLife.WorldOffset += new Vector2(220f, -120f);

		// Additional detached wreck modules (free-floating nearby).
		//var wreckDetached1 = AddModule(ModuleSizePreset.Small, wreckBase + new Vector2(-260f, 180f), isDerelict: true);
		//var wreckDetached2 = AddModule(ModuleSizePreset.Medium, wreckBase + new Vector2(360f, 240f), isDerelict: true);

		// Debug-time overlap check (world AABB approximation). Linked modules should touch at doors but not overlap.
		for (var i = 0; i < _modules.Count; i++)
		{
			for (var j = i + 1; j < _modules.Count; j++)
			{
				var aabbA = GetWorldAabb(_modules[i]);
				var aabbB = GetWorldAabb(_modules[j]);
				if (AabbIntersects(aabbA, aabbB))
				{
					Debug.WriteLine($"[BuildScene] WARNING: module AABB overlap #{_modules[i].ModuleId} vs #{_modules[j].ModuleId}");
				}
			}
		}

		// --- Debris / space obstacles ---
		SeedDebrisObstacles(
			starterCenter: starterCommand.GetWorldCenter(),
			wreckCenter: wreckCommand.GetWorldCenter());

		// Treat detached derelict modules as EVA obstacles (keeps detours interesting).
		//AddModuleAsObstacleIfDetached(wreckLife);
		//AddModuleAsObstacleIfDetached(wreckDetached1);
		//AddModuleAsObstacleIfDetached(wreckDetached2);
	}

	private void AddModuleAsObstacleIfDetached(ShipModuleInstance module)
	{
		var linked = false;
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			if (ModuleGraph.TryGetLink(module.ModuleId, side, out _))
			{
				linked = true;
				break;
			}
		}
		if (linked)
		{
			return;
		}

		var center = module.GetWorldCenter();
		var r = ApproxModuleRadius(module) * 0.75f;
		_spaceObstacles.Add(new CircleObstacle(center, MathF.Max(30f, r)));
	}

	private void SeedDebrisObstacles(Vector2 starterCenter, Vector2 wreckCenter)
	{
		// Seeded random so the demo scene is repeatable.
		var rng = new Random(1337);
		var dir = wreckCenter - starterCenter;
		var len = MathF.Max(1f, dir.Length());
		var n = dir / len;
		var perp = new Vector2(-n.Y, n.X);

		bool IsTooCloseToAnyModule(Vector2 p, float radius)
		{
			for (var i = 0; i < _modules.Count; i++)
			{
				var m = _modules[i];
				var c = m.GetWorldCenter();
				var r = ApproxModuleRadius(m) + radius + 24f;
				if (Vector2.DistanceSquared(p, c) <= r * r)
				{
					return true;
				}
			}
			return false;
		}

		// Debris between starter and wreck.
		var desired = 26;
		var tries = 0;
		while (_spaceObstacles.Count < desired && tries++ < 800)
		{
			var t = 0.12f + (float)rng.NextDouble() * 0.76f;
			var along = starterCenter + n * (len * t);
			var lateral = ((float)rng.NextDouble() - 0.5f) * 520f;
			var p = along + perp * lateral;
			var radius = 18f + (float)rng.NextDouble() * 32f;
			if (IsTooCloseToAnyModule(p, radius))
			{
				continue;
			}
			_spaceObstacles.Add(new CircleObstacle(p, radius));
		}

		// Extra debris around the wreck.
		var around = 16;
		tries = 0;
		while (_spaceObstacles.Count < desired + around && tries++ < 900)
		{
			var angle = (float)rng.NextDouble() * MathF.Tau;
			var dist = 160f + (float)rng.NextDouble() * 620f;
			var p2 = wreckCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
			var radius2 = 16f + (float)rng.NextDouble() * 28f;
			if (IsTooCloseToAnyModule(p2, radius2))
			{
				continue;
			}
			_spaceObstacles.Add(new CircleObstacle(p2, radius2));
		}
	}

	private static float ApproxModuleRadius(ShipModuleInstance module)
	{
		var maxDim = Math.Max(module.Width, module.Height);
		return (MathF.Max(1, maxDim - 1) * (IsoMath.TileWidth / 4f)) + 70f;
	}

	private void ValidateDoorLinks()
	{
		var links = ModuleGraph.EnumerateUniqueLinksSnapshot();
		for (var i = 0; i < links.Count; i++)
		{
			var (a, b) = links[i];
			var ma = GetModuleById(a.ModuleId);
			var mb = GetModuleById(b.ModuleId);
			if (ma is null || mb is null)
			{
				ModuleGraph.UnlinkDoor(a.ModuleId, a.Side);
				continue;
			}

			var pa = ma.GetDoorWorldPos(a.Side);
			var pb = mb.GetDoorWorldPos(b.Side);
			var d = Vector2.Distance(pa, pb);
			var expected = ShipModuleInstance.GetExpectedDoorSeamDistance(a.Side);
			// Links are valid when doors are adjacent across a seam (distance ~= one tile step).
			// Break only when the deviation from expected seam distance is too large.
			if (MathF.Abs(d - expected) > DoorLinkBreakDistance)
			{
				ModuleGraph.UnlinkDoor(a.ModuleId, a.Side);
			}
		}
	}

	private Vector2 ComputeSpawnAfterPlacement()
	{
		// Spawn point is separate from room modules. For now: center of the first generated room
		// after all rooms have been placed into the scene.
		var first = _modules[0];
		var gx = first.OriginX + (first.Width / 2);
		var gy = first.OriginY + (first.Height / 2);

		// Ensure we land on a walkable cell.
		var candidates = new (int dx, int dy)[]
		{
			(0, 0), (1, 0), (-1, 0), (0, 1), (0, -1),
			(1, 1), (-1, 1), (1, -1), (-1, -1),
		};
		for (var i = 0; i < candidates.Length; i++)
		{
			var c = candidates[i];
			var tx = gx + c.dx;
			var ty = gy + c.dy;
			for (var m = 0; m < _moduleMaps.Count; m++)
			{
				if (_moduleMaps[m].IsWalkableCell(tx, ty))
				{
					return IsoMath.GridToWorld(tx, ty) + _modules[0].WorldOffset;
				}
			}
		}

		// Fallback: still return the computed center.
		return IsoMath.GridToWorld(gx, gy) + _modules[0].WorldOffset;
	}
}
