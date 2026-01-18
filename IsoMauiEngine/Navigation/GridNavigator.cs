using System.Numerics;
using IsoMauiEngine.Iso;
using IsoMauiEngine.World;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.Navigation;

public sealed class GridNavigator : INavigator
{
	private readonly GameWorld _world;

	public GridNavigator(GameWorld world)
	{
		_world = world;
	}

	public NavPath ComputePath(NavRequest request)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(request.StartWorld, out var startModule, out var startCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNav: start not inside module";
			return path;
		}
		if (!_world.TryFindContainingModule(request.TargetWorld, out var goalModule, out var goalCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNav: target not inside module";
			return path;
		}

		var modulesRoute = _world.ModuleGraph.FindModuleRoute(startModule.ModuleId, goalModule.ModuleId);
		if (modulesRoute.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNav: no module route";
			return path;
		}

		var debugHops = new List<string>();
		var waypointCells = new List<(ShipModuleInstance module, AStarGrid.Cell cell)>();

		// Single-module path.
		if (modulesRoute.Count == 1)
		{
			var local = FindLocalPath(startModule, startCell, goalCell);
			if (local.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNav: no local path";
				return path;
			}
			foreach (var c in local)
			{
				waypointCells.Add((startModule, c));
			}
			FinalizeWorldWaypoints(path, waypointCells);
			path.IsValid = true;
			path.DebugInfo = "GridNav: local";
			return path;
		}

		// Multi-module: local A* segments chained via door links.
		var currentModule = startModule;
		var currentCell = startCell;

		for (var i = 0; i < modulesRoute.Count - 1; i++)
		{
			var fromId = modulesRoute[i];
			var toId = modulesRoute[i + 1];
			var fromModule = _world.GetModuleById(fromId);
			var toModule = _world.GetModuleById(toId);
			if (fromModule is null || toModule is null)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNav: missing module instance";
				return path;
			}

			if (!TryFindLinkedDoor(fromModule, toModule, out var exitSide, out var entrySide))
			{
				path.IsValid = false;
				path.DebugInfo = "GridNav: modules not linked";
				return path;
			}

			debugHops.Add($"{fromModule.ModuleId}:{exitSide}->{toModule.ModuleId}:{entrySide}");

			var exitCell = fromModule.GetDoorWorldCell(exitSide);
			var exitLocal = FindLocalPath(fromModule, currentCell, new AStarGrid.Cell(exitCell.x, exitCell.y));
			if (exitLocal.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNav: no path to exit door";
				return path;
			}
			foreach (var c in exitLocal)
			{
				waypointCells.Add((fromModule, c));
			}

			var entryCell = toModule.GetDoorWorldCell(entrySide);
			currentModule = toModule;
			currentCell = new AStarGrid.Cell(entryCell.x, entryCell.y);
		}

		// Final segment inside goal module.
		var finalLocal = FindLocalPath(currentModule, currentCell, goalCell);
		if (finalLocal.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNav: no final local path";
			return path;
		}
		foreach (var c in finalLocal)
		{
			waypointCells.Add((currentModule, c));
		}

		FinalizeWorldWaypoints(path, waypointCells);
		path.IsValid = true;
		path.DebugInfo = $"GridNav: hops={string.Join(",", debugHops)}";
		return path;
	}

	public NavPath ComputeExitToNearestDoor(Vector2 startWorld)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(startWorld, out var module, out var startCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavExit: start not inside module";
			return path;
		}

		var best = (List<AStarGrid.Cell>?)null;
		DoorSide bestSide = DoorSide.North;
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			var door = module.GetDoorWorldCell(side);
			var candidate = FindLocalPath(module, startCell, new AStarGrid.Cell(door.x, door.y));
			if (candidate.Count == 0)
			{
				continue;
			}
			if (best is null || candidate.Count < best.Count)
			{
				best = candidate;
				bestSide = side;
			}
		}

		if (best is null)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavExit: no door path";
			return path;
		}

		var cells = new List<(ShipModuleInstance module, AStarGrid.Cell cell)>(best.Count);
		for (var i = 0; i < best.Count; i++)
		{
			cells.Add((module, best[i]));
		}
		FinalizeWorldWaypoints(path, cells);
		path.IsValid = true;
		path.DebugInfo = $"GridNavExit: {bestSide}";
		return path;
	}

	public NavPath ComputeEntryFromNearestDoor(Vector2 targetWorld)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(targetWorld, out var module, out var goalCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: target not inside module";
			return path;
		}

		// Choose nearest door cell to the goal.
		var bestDoor = new AStarGrid.Cell();
		var bestDist = int.MaxValue;
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			var door = module.GetDoorWorldCell(side);
			var d = Math.Abs(door.x - goalCell.X) + Math.Abs(door.y - goalCell.Y);
			if (d < bestDist)
			{
				bestDist = d;
				bestDoor = new AStarGrid.Cell(door.x, door.y);
			}
		}

		var local = FindLocalPath(module, bestDoor, goalCell);
		if (local.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: no local path";
			return path;
		}

		var cells = new List<(ShipModuleInstance module, AStarGrid.Cell cell)>(local.Count);
		for (var i = 0; i < local.Count; i++)
		{
			cells.Add((module, local[i]));
		}
		FinalizeWorldWaypoints(path, cells);
		path.IsValid = true;
		path.DebugInfo = "GridNavEnter";
		return path;
	}

	private List<AStarGrid.Cell> FindLocalPath(ShipModuleInstance module, AStarGrid.Cell start, AStarGrid.Cell goal)
	{
		var minX = module.OriginX;
		var minY = module.OriginY;
		var maxX = module.OriginX + module.Width - 1;
		var maxY = module.OriginY + module.Height - 1;
		return AStarGrid.FindPath(
			start,
			goal,
			isWalkable: (x, y) => _world.IsWalkableCellInModule(module, x, y),
			isInBounds: (x, y) => x >= minX && x <= maxX && y >= minY && y <= maxY);
	}

	private static void FinalizeWorldWaypoints(NavPath path, List<(ShipModuleInstance module, AStarGrid.Cell cell)> cells)
	{
		path.Waypoints.Clear();
		Vector2? last = null;
		for (var i = 0; i < cells.Count; i++)
		{
			var (m, c) = cells[i];
			var w = IsoMath.GridToWorld(c.X, c.Y) + m.WorldOffset;
			// De-dupe consecutive duplicates (common when chaining segments).
			if (last.HasValue && Vector2.DistanceSquared(last.Value, w) < 0.0001f)
			{
				continue;
			}
			path.Waypoints.Add(w);
			last = w;
		}
	}

	private bool TryFindLinkedDoor(ShipModuleInstance from, ShipModuleInstance to, out DoorSide exitSide, out DoorSide entrySide)
	{
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			if (_world.ModuleGraph.TryGetLink(from.ModuleId, side, out var link) && link.OtherModuleId == to.ModuleId)
			{
				exitSide = side;
				entrySide = link.OtherSide;
				return true;
			}
		}

		exitSide = DoorSide.North;
		entrySide = DoorSide.South;
		return false;
	}
}
