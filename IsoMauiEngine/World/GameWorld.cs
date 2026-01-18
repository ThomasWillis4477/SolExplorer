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
	private readonly List<Entity> _entities = new();
	private readonly List<ShipModuleInstance> _modules = new();
	private readonly List<ModuleGridMap> _moduleMaps = new();
	private readonly Vector2 _spawnWorldPos;
	private readonly List<CircleObstacle> _spaceObstacles = new();

	public GameWorld()
	{
		BuildScene();
		_spawnWorldPos = ComputeSpawnAfterPlacement();

		Player = new Player
		{
			WorldPos = _spawnWorldPos
		};

		_entities.Add(Player);

		ModuleGraph = new ModuleGraph();
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
			return TryGetCellKind(nextModule, nextCell, out var k) && k == CellKind.Door;
		}

		// Module -> space: only allow exiting via a door tile.
		if (curInside && !nextInside)
		{
			return TryGetCellKind(curModule, curCell, out var k) && k == CellKind.Door;
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
				return _moduleMaps[i].IsWalkableCell(worldGridX, worldGridY);
			}
		}
		return false;
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

		// First generated room (Medium) at origin for now.
		var firstBlueprint = BlueprintLibrary.Get(ModuleSizePreset.Medium);
		var first = new ShipModuleInstance(firstBlueprint, ModuleSizePreset.Medium, originX: 0, originY: 0);
		_modules.Add(first);
		_moduleMaps.Add(new ModuleGridMap(first));

		// Default: no links yet (single module).

		// Future: add more rooms here, then spawn selection uses the first room after placement.
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
