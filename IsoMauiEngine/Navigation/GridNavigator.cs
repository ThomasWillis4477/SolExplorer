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

	/// <summary>
	/// When the player clicks a target in space while inside a module, route to an airlock module
	/// (chosen by proximity to the clicked point), then route to an airlock door that is not the
	/// entry door used to enter the airlock (prefer an unlinked door to exit to space).
	/// </summary>
	public NavPath ComputeExitViaAirlock(Vector2 startWorld, Vector2 targetWorld)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(startWorld, out var startModule, out var startCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavExit: start not inside module";
			return path;
		}

		ShipModuleInstance? chosenAirlock = null;
		List<int>? chosenRoute = null;
		var bestDist2 = float.PositiveInfinity;
		for (var i = 0; i < _world.Modules.Count; i++)
		{
			var candidate = _world.Modules[i];
			if (!candidate.IsAirlock)
			{
				continue;
			}
			var route = _world.ModuleGraph.FindModuleRoute(startModule.ModuleId, candidate.ModuleId);
			if (route.Count == 0)
			{
				continue;
			}
			var d2 = Vector2.DistanceSquared(candidate.GetWorldCenter(), targetWorld);
			if (d2 < bestDist2)
			{
				bestDist2 = d2;
				chosenAirlock = candidate;
				chosenRoute = route;
			}
		}

		if (chosenAirlock is null || chosenRoute is null)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavExit: no reachable airlock";
			return path;
		}

		// Determine the airlock entry door side (if we are entering the airlock from another module).
		DoorSide? entrySide = null;
		if (chosenRoute.Count >= 2)
		{
			var prevId = chosenRoute[^2];
			var prevModule = _world.GetModuleById(prevId);
			if (prevModule is not null && TryFindLinkedDoor(prevModule, chosenAirlock, out _, out var airlockEntry))
			{
				entrySide = airlockEntry;
			}
		}

		// Choose an airlock exit door that is not the entry door.
		// Prefer an unlinked door (to space), then prefer the door closest to the clicked point in space.
		DoorSide exitSide = DoorSide.North;
		var bestExitIsUnlinked = false;
		var bestExitDist2 = float.PositiveInfinity;
		var exitSideSet = false;
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			if (entrySide.HasValue && side == entrySide.Value)
			{
				continue;
			}
			var isUnlinked = !_world.ModuleGraph.TryGetLink(chosenAirlock.ModuleId, side, out _);
			var d2 = Vector2.DistanceSquared(chosenAirlock.GetDoorWorldPos(side), targetWorld);
			if (!exitSideSet
				|| (isUnlinked && !bestExitIsUnlinked)
				|| (isUnlinked == bestExitIsUnlinked && d2 < bestExitDist2))
			{
				exitSide = side;
				bestExitIsUnlinked = isUnlinked;
				bestExitDist2 = d2;
				exitSideSet = true;
			}
		}

		var waypointCells = new List<(ShipModuleInstance module, AStarGrid.Cell cell)>();

		// Already in airlock: just go to chosen exit door.
		if (chosenRoute.Count == 1)
		{
			var exitCell = chosenAirlock.GetDoorWorldCell(exitSide);
			var local = FindLocalPath(chosenAirlock, startCell, new AStarGrid.Cell(exitCell.x, exitCell.y));
			if (local.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavExit: no path to airlock exit";
				return path;
			}
			for (var i = 0; i < local.Count; i++)
			{
				waypointCells.Add((chosenAirlock, local[i]));
			}
			FinalizeWorldWaypoints(path, waypointCells);
			path.IsValid = true;
			path.DebugInfo = $"GridNavExit: airlock#{chosenAirlock.ModuleId} exit={exitSide}";
			return path;
		}

		// Multi-module: chain local segments through door links, ending inside the airlock.
		var currentCell = startCell;
		for (var i = 0; i < chosenRoute.Count - 1; i++)
		{
			var fromId = chosenRoute[i];
			var toId = chosenRoute[i + 1];
			var fromModule = _world.GetModuleById(fromId);
			var toModule = _world.GetModuleById(toId);
			if (fromModule is null || toModule is null)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavExit: missing module instance";
				return path;
			}

			if (!TryFindLinkedDoor(fromModule, toModule, out var exitHopSide, out var entryHopSide))
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavExit: modules not linked";
				return path;
			}

			var exitHop = fromModule.GetDoorWorldCell(exitHopSide);
			var exitLocal = FindLocalPath(fromModule, currentCell, new AStarGrid.Cell(exitHop.x, exitHop.y));
			if (exitLocal.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavExit: no path to linked door";
				return path;
			}
			for (var c = 0; c < exitLocal.Count; c++)
			{
				waypointCells.Add((fromModule, exitLocal[c]));
			}

			var entryHop = toModule.GetDoorWorldCell(entryHopSide);
			currentCell = new AStarGrid.Cell(entryHop.x, entryHop.y);
		}

		// Final segment: within airlock, go to chosen exit door (not entrySide).
		var exit = chosenAirlock.GetDoorWorldCell(exitSide);
		var finalLocal = FindLocalPath(chosenAirlock, currentCell, new AStarGrid.Cell(exit.x, exit.y));
		if (finalLocal.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavExit: no path to airlock exit";
			return path;
		}
		for (var c = 0; c < finalLocal.Count; c++)
		{
			waypointCells.Add((chosenAirlock, finalLocal[c]));
		}

		FinalizeWorldWaypoints(path, waypointCells);
		path.IsValid = true;
		path.DebugInfo = $"GridNavExit: airlock#{chosenAirlock.ModuleId} exit={exitSide}";
		return path;
	}

	public NavPath ComputeEntryFromNearestDoor(Vector2 startWorld, Vector2 targetWorld)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(targetWorld, out var module, out var goalCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: target not inside module";
			return path;
		}

		// Choose the entry door for space->module:
		// - must be walkable (door tile passable)
		// - prefer an unlinked door (an exterior/undocked door)
		// - prefer the door closest to the player's current space position
		// - tie-break by proximity to the clicked goal inside the module
		AStarGrid.Cell bestDoor = default;
		DoorSide bestSide = DoorSide.North;
		var bestIsLinked = true;
		var bestSpaceDist2 = float.PositiveInfinity;
		var bestGoalDist = int.MaxValue;
		var bestSet = false;
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			var door = module.GetDoorWorldCell(side);
			if (!_world.IsWalkableCellInModule(module, door.x, door.y))
			{
				continue;
			}

			var isLinked = _world.ModuleGraph.TryGetLink(module.ModuleId, side, out _);
			var spaceD2 = Vector2.DistanceSquared(module.GetDoorWorldPos(side), startWorld);
			var goalD = Math.Abs(door.x - goalCell.X) + Math.Abs(door.y - goalCell.Y);

			var better = false;
			if (!bestSet)
			{
				better = true;
			}
			else if (!isLinked && bestIsLinked)
			{
				better = true;
			}
			else if (isLinked == bestIsLinked)
			{
				if (spaceD2 < bestSpaceDist2)
				{
					better = true;
				}
				else if (Math.Abs(spaceD2 - bestSpaceDist2) < 0.0001f && goalD < bestGoalDist)
				{
					better = true;
				}
			}

			if (!better)
			{
				continue;
			}

			bestSet = true;
			bestIsLinked = isLinked;
			bestSpaceDist2 = spaceD2;
			bestGoalDist = goalD;
			bestSide = side;
			bestDoor = new AStarGrid.Cell(door.x, door.y);
		}

		if (!bestSet)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: no walkable door";
			return path;
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
		path.DebugInfo = $"GridNavEnter: side={bestSide} linked={bestIsLinked}";
		return path;
	}

	/// <summary>
	/// When the player clicks inside a module while in space, approach the nearest walkable unlinked
	/// airlock exterior door, then route through linked modules to the clicked target.
	/// </summary>
	public NavPath ComputeEntryViaAirlock(Vector2 startWorld, Vector2 targetWorld)
	{
		var path = new NavPath();
		if (!_world.TryFindContainingModule(targetWorld, out var targetModule, out var goalCell))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: target not inside module";
			return path;
		}

		ShipModuleInstance? chosenAirlock = null;
		List<int>? chosenRoute = null;
		DoorSide chosenExteriorSide = DoorSide.North;
		AStarGrid.Cell chosenExteriorDoorCell = default;
		var bestDoorDist2 = float.PositiveInfinity;
		var bestTargetDist2 = float.PositiveInfinity;
		var found = false;

		for (var i = 0; i < _world.Modules.Count; i++)
		{
			var airlock = _world.Modules[i];
			if (!airlock.IsAirlock)
			{
				continue;
			}

			var route = _world.ModuleGraph.FindModuleRoute(airlock.ModuleId, targetModule.ModuleId);
			if (route.Count == 0)
			{
				continue;
			}

			// Pick the best exterior (unlinked) door on this airlock.
			var localFound = false;
			DoorSide bestSide = DoorSide.North;
			AStarGrid.Cell bestDoor = default;
			var localBestD2 = float.PositiveInfinity;
			foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
			{
				// Exterior door = not linked.
				if (_world.ModuleGraph.TryGetLink(airlock.ModuleId, side, out _))
				{
					continue;
				}
				var door = airlock.GetDoorWorldCell(side);
				if (!_world.IsWalkableCellInModule(airlock, door.x, door.y))
				{
					continue;
				}
				var d2 = Vector2.DistanceSquared(airlock.GetDoorWorldPos(side), startWorld);
				if (!localFound || d2 < localBestD2)
				{
					localFound = true;
					localBestD2 = d2;
					bestSide = side;
					bestDoor = new AStarGrid.Cell(door.x, door.y);
				}
			}

			if (!localFound)
			{
				continue;
			}

			var targetD2 = Vector2.DistanceSquared(airlock.GetWorldCenter(), targetWorld);
			if (!found || localBestD2 < bestDoorDist2 || (Math.Abs(localBestD2 - bestDoorDist2) < 0.0001f && targetD2 < bestTargetDist2))
			{
				found = true;
				bestDoorDist2 = localBestD2;
				bestTargetDist2 = targetD2;
				chosenAirlock = airlock;
				chosenRoute = route;
				chosenExteriorSide = bestSide;
				chosenExteriorDoorCell = bestDoor;
			}
		}

		if (!found || chosenAirlock is null || chosenRoute is null)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: no reachable airlock exterior door";
			return path;
		}

		var waypointCells = new List<(ShipModuleInstance module, AStarGrid.Cell cell)>();

		// If the target is inside the airlock itself, route from exterior door to goal.
		if (chosenRoute.Count == 1)
		{
			var local = FindLocalPath(chosenAirlock, chosenExteriorDoorCell, goalCell);
			if (local.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavEnter: no local path from exterior door";
				return path;
			}
			for (var i = 0; i < local.Count; i++)
			{
				waypointCells.Add((chosenAirlock, local[i]));
			}
			FinalizeWorldWaypoints(path, waypointCells);
			path.IsValid = true;
			path.DebugInfo = $"GridNavEnter: airlock#{chosenAirlock.ModuleId} ext={chosenExteriorSide}";
			return path;
		}

		// First: within airlock, go from exterior door to the linked door leading to the next module.
		var nextId = chosenRoute[1];
		var nextModule = _world.GetModuleById(nextId);
		if (nextModule is null)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: missing next module";
			return path;
		}
		if (!TryFindLinkedDoor(chosenAirlock, nextModule, out var airlockExitSide, out var nextEntrySide))
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: airlock not linked to route";
			return path;
		}
		var airlockExit = chosenAirlock.GetDoorWorldCell(airlockExitSide);
		var airlockLocal = FindLocalPath(chosenAirlock, chosenExteriorDoorCell, new AStarGrid.Cell(airlockExit.x, airlockExit.y));
		if (airlockLocal.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: no path from exterior to linked door";
			return path;
		}
		for (var i = 0; i < airlockLocal.Count; i++)
		{
			waypointCells.Add((chosenAirlock, airlockLocal[i]));
		}

		// Then: chain through modules along the route until we reach the target module.
		var entryCellWorld = nextModule.GetDoorWorldCell(nextEntrySide);
		var currentCell = new AStarGrid.Cell(entryCellWorld.x, entryCellWorld.y);
		for (var i = 1; i < chosenRoute.Count - 1; i++)
		{
			var fromId = chosenRoute[i];
			var toId = chosenRoute[i + 1];
			var fromModule = _world.GetModuleById(fromId);
			var toModule = _world.GetModuleById(toId);
			if (fromModule is null || toModule is null)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavEnter: missing module instance";
				return path;
			}
			if (!TryFindLinkedDoor(fromModule, toModule, out var exitHopSide, out var entryHopSide))
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavEnter: modules not linked";
				return path;
			}
			var exitHop = fromModule.GetDoorWorldCell(exitHopSide);
			var segment = FindLocalPath(fromModule, currentCell, new AStarGrid.Cell(exitHop.x, exitHop.y));
			if (segment.Count == 0)
			{
				path.IsValid = false;
				path.DebugInfo = "GridNavEnter: no path to linked door";
				return path;
			}
			for (var c = 0; c < segment.Count; c++)
			{
				waypointCells.Add((fromModule, segment[c]));
			}
			var entryHop = toModule.GetDoorWorldCell(entryHopSide);
			currentCell = new AStarGrid.Cell(entryHop.x, entryHop.y);
		}

		// Final: inside target module, route from entry door cell to goal.
		var finalLocal = FindLocalPath(targetModule, currentCell, goalCell);
		if (finalLocal.Count == 0)
		{
			path.IsValid = false;
			path.DebugInfo = "GridNavEnter: no local path to goal";
			return path;
		}
		for (var i = 0; i < finalLocal.Count; i++)
		{
			waypointCells.Add((targetModule, finalLocal[i]));
		}

		FinalizeWorldWaypoints(path, waypointCells);
		path.IsValid = true;
		path.DebugInfo = $"GridNavEnter: airlock#{chosenAirlock.ModuleId} ext={chosenExteriorSide}";
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
