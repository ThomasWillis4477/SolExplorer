using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Entities;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.World;

public sealed class GameWorld
{
	private readonly List<Entity> _entities = new();
	private readonly List<ShipModuleInstance> _modules = new();
	private readonly List<ModuleGridMap> _moduleMaps = new();
	private readonly Vector2 _spawnWorldPos;

	public GameWorld()
	{
		BuildScene();
		_spawnWorldPos = ComputeSpawnAfterPlacement();

		Player = new Player
		{
			WorldPos = _spawnWorldPos
		};

		Player.CanMoveToWorld = IsWalkableWorldPos;

		_entities.Add(Player);
	}

	/// <summary>
	/// All placed modules in the scene (order is placement order).
	/// </summary>
	public IReadOnlyList<ShipModuleInstance> Modules => _modules;

	/// <summary>
	/// Back-compat: first module (the "first generated room").
	/// </summary>
	public ShipModuleInstance Module => _modules[0];

	public Player Player { get; }

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

	private bool IsWalkableWorldPos(Vector2 worldPos)
	{
		var g = IsoMath.WorldToGrid(worldPos);
		var gx = (int)MathF.Round(g.X);
		var gy = (int)MathF.Round(g.Y);
		for (var i = 0; i < _moduleMaps.Count; i++)
		{
			if (_moduleMaps[i].IsWalkableCell(gx, gy))
			{
				return true;
			}
		}
		return false;
	}

	private void BuildScene()
	{
		_modules.Clear();
		_moduleMaps.Clear();

		// First generated room (Medium) at origin for now.
		var firstBlueprint = BlueprintLibrary.Get(ModuleSizePreset.Medium);
		var first = new ShipModuleInstance(firstBlueprint, originX: 0, originY: 0);
		_modules.Add(first);
		_moduleMaps.Add(new ModuleGridMap(first));

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
					return IsoMath.GridToWorld(tx, ty);
				}
			}
		}

		// Fallback: still return the computed center.
		return IsoMath.GridToWorld(gx, gy);
	}
}
