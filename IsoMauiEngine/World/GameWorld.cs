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

	public void AppendDrawItems(List<DrawItem> drawItems)
	{
		for (var i = 0; i < _moduleMaps.Count; i++)
		{
			_moduleMaps[i].AppendDrawItems(drawItems);
		}
		for (var i = 0; i < _entities.Count; i++)
		{
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

		ShipModuleInstance AddModule(ModuleSizePreset preset, Vector2 worldOffset, bool isDerelict)
		{
			var bp = BlueprintLibrary.Get(preset);
			var m = new ShipModuleInstance(bp, preset, originX: 0, originY: 0)
			{
				WorldOffset = worldOffset,
				IsDerelict = isDerelict
			};
			_modules.Add(m);
			_moduleMaps.Add(new ModuleGridMap(m));
			return m;
		}

		void AlignAndLink(ShipModuleInstance a, DoorSide sideA, ShipModuleInstance b, DoorSide sideB, float maxAlignDistance)
		{
			// Align B so its door matches A's door, then link the doors.
			var aDoor = a.GetDoorWorldPos(sideA);
			var bDoor = b.GetDoorWorldPos(sideB);
			b.WorldOffset += (aDoor - bDoor);

			var bDoor2 = b.GetDoorWorldPos(sideB);
			if (Vector2.Distance(aDoor, bDoor2) <= maxAlignDistance)
			{
				ModuleGraph.TryLinkDoors(a.ModuleId, sideA, b.ModuleId, sideB);
			}
		}

		// --- Starter ship (near origin) ---
		var starterOrigin = Vector2.Zero;
		var starterCommand = AddModule(ModuleSizePreset.Medium, starterOrigin, isDerelict: false);
		var starterCargo = AddModule(ModuleSizePreset.Medium, starterOrigin, isDerelict: false);
		var starterAirlock = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false);
		starterAirlock.IsAirlock = true;
		var starterGenerator = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false);
		var starterLifeSupport = AddModule(ModuleSizePreset.Small, starterOrigin, isDerelict: false);
		var starterEngine = AddModule(ModuleSizePreset.Medium, starterOrigin, isDerelict: false);

		// Link/align modules (example graph):
		// Command East <-> Cargo West
		// Command South <-> Airlock North
		// Cargo South <-> Generator North
		// Cargo East <-> LifeSupport West
		// Command West <-> Engine East (optional)
		AlignAndLink(starterCommand, DoorSide.East, starterCargo, DoorSide.West, maxAlignDistance: 1f);
		AlignAndLink(starterCommand, DoorSide.South, starterAirlock, DoorSide.North, maxAlignDistance: 1f);
		AlignAndLink(starterCargo, DoorSide.South, starterGenerator, DoorSide.North, maxAlignDistance: 1f);
		AlignAndLink(starterCargo, DoorSide.East, starterLifeSupport, DoorSide.West, maxAlignDistance: 1f);
		AlignAndLink(starterCommand, DoorSide.West, starterEngine, DoorSide.East, maxAlignDistance: 1f);

		// --- Wreck (far away) ---
		var wreckBase = new Vector2(2600f, 1400f);
		var wreckCommand = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);
		var wreckCargo = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);
		var wreckGen = AddModule(ModuleSizePreset.Small, wreckBase, isDerelict: true);
		var wreckLife = AddModule(ModuleSizePreset.Small, wreckBase, isDerelict: true);
		var wreckEngine = AddModule(ModuleSizePreset.Medium, wreckBase, isDerelict: true);

		// Partial linked cluster in the wreck.
		AlignAndLink(wreckCommand, DoorSide.East, wreckCargo, DoorSide.West, maxAlignDistance: 1f);
		AlignAndLink(wreckCargo, DoorSide.South, wreckGen, DoorSide.North, maxAlignDistance: 1f);
		AlignAndLink(wreckCommand, DoorSide.West, wreckEngine, DoorSide.East, maxAlignDistance: 1f);
		// Leave wreckLife detached but nearby.
		wreckLife.WorldOffset += new Vector2(220f, -120f);

		// Additional detached wreck modules (free-floating nearby).
		var wreckDetached1 = AddModule(ModuleSizePreset.Small, wreckBase + new Vector2(-260f, 180f), isDerelict: true);
		var wreckDetached2 = AddModule(ModuleSizePreset.Medium, wreckBase + new Vector2(360f, 240f), isDerelict: true);

		// --- Debris / space obstacles ---
		SeedDebrisObstacles(
			starterCenter: starterCommand.GetWorldCenter(),
			wreckCenter: wreckCommand.GetWorldCenter());

		// Treat detached derelict modules as EVA obstacles (keeps detours interesting).
		AddModuleAsObstacleIfDetached(wreckLife);
		AddModuleAsObstacleIfDetached(wreckDetached1);
		AddModuleAsObstacleIfDetached(wreckDetached2);
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
			var p = wreckCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
			var radius = 16f + (float)rng.NextDouble() * 28f;
			if (IsTooCloseToAnyModule(p, radius))
			{
				continue;
			}
			_spaceObstacles.Add(new CircleObstacle(p, radius));
		}
	}

	private static float ApproxModuleRadius(ShipModuleInstance module)
	{
		// Cheap bound in world units. (No rotation.)
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
			if (Vector2.Distance(pa, pb) > DoorLinkBreakDistance)
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
