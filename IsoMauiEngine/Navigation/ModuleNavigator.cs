using System.Numerics;
using IsoMauiEngine.World;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.Navigation;

public sealed class ModuleNavigator : INavigator
{
	private readonly GameWorld _world;
	private readonly float _clearance;
	private readonly float _snapTolerance;

	public ModuleNavigator(GameWorld world, float clearance = 24f, float snapTolerance = 18f)
	{
		_world = world;
		_clearance = clearance;
		_snapTolerance = snapTolerance;
	}

	public NavPath ComputePath(NavRequest request)
	{
		var path = new NavPath();
		var start = request.StartWorld;
		var target = request.TargetWorld;
		var obstacles = _world.SpaceObstacles;

		// Same as SpaceNav: LOS + one detour.
		var blocking = FindFirstBlocking(start, target, obstacles);
		if (!blocking.HasValue)
		{
			path.IsValid = true;
			path.Waypoints.Add(target);
			path.DebugInfo = "ModuleNav: LOS";
			return path;
		}

		var obstacle = blocking.Value;
		var detour = ComputeDetour(start, target, obstacle, obstacles);
		path.IsValid = true;
		path.Waypoints.Add(detour);
		path.Waypoints.Add(target);
		path.DebugInfo = "ModuleNav: detour";
		return path;
	}

	public bool TrySnapDock(ShipModuleInstance moving)
	{
		// Snap-docking: if any moving door is within tolerance of an opposite door, align and link.
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			var doorPos = moving.GetDoorWorldPos(side);
			var seamStep = ShipModuleInstance.GetWorldStepForSide(side);
			var opposite = side.Opposite();
			for (var i = 0; i < _world.Modules.Count; i++)
			{
				var other = _world.Modules[i];
				if (other.ModuleId == moving.ModuleId)
				{
					continue;
				}
				var otherDoorPos = other.GetDoorWorldPos(opposite);
				// Doors should end up adjacent across the seam, not coincident.
				var desiredOtherDoorPos = doorPos + seamStep;
				if (Vector2.Distance(desiredOtherDoorPos, otherDoorPos) <= _snapTolerance)
				{
					var desiredDoorPos = otherDoorPos - seamStep;
					var delta = desiredDoorPos - doorPos;
					moving.WorldOffset += delta;
					_world.ModuleGraph.TryLinkDoors(moving.ModuleId, side, other.ModuleId, opposite);
					return true;
				}
			}
		}
		return false;
	}

	private CircleObstacle? FindFirstBlocking(Vector2 a, Vector2 b, IReadOnlyList<CircleObstacle> obstacles)
	{
		for (var i = 0; i < obstacles.Count; i++)
		{
			if (ObstacleMath.SegmentIntersectsCircle(a, b, obstacles[i]))
			{
				return obstacles[i];
			}
		}
		return null;
	}

	private Vector2 ComputeDetour(Vector2 start, Vector2 target, CircleObstacle obstacle, IReadOnlyList<CircleObstacle> obstacles)
	{
		var dir = target - start;
		if (dir.LengthSquared() < 1e-5f)
		{
			return target;
		}
		dir = Vector2.Normalize(dir);
		var perp = new Vector2(-dir.Y, dir.X);
		var dist = obstacle.Radius + _clearance;

		var left = obstacle.Center + perp * dist;
		var right = obstacle.Center - perp * dist;

		var leftOk = !AnyBlocks(start, left, obstacles) && !AnyBlocks(left, target, obstacles);
		var rightOk = !AnyBlocks(start, right, obstacles) && !AnyBlocks(right, target, obstacles);

		if (leftOk && rightOk)
		{
			var dl = Vector2.Distance(start, left) + Vector2.Distance(left, target);
			var dr = Vector2.Distance(start, right) + Vector2.Distance(right, target);
			return dl <= dr ? left : right;
		}
		if (leftOk) return left;
		if (rightOk) return right;
		return left;
	}

	private static bool AnyBlocks(Vector2 a, Vector2 b, IReadOnlyList<CircleObstacle> obstacles)
	{
		for (var i = 0; i < obstacles.Count; i++)
		{
			if (ObstacleMath.SegmentIntersectsCircle(a, b, obstacles[i]))
			{
				return true;
			}
		}
		return false;
	}
}
