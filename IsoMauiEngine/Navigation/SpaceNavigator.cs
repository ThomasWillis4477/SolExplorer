using System.Numerics;
using IsoMauiEngine.World;

namespace IsoMauiEngine.Navigation;

public sealed class SpaceNavigator : INavigator
{
	private readonly GameWorld _world;
	private readonly float _clearance;

	public SpaceNavigator(GameWorld world, float clearance = 24f)
	{
		_world = world;
		_clearance = clearance;
	}

	public NavPath ComputePath(NavRequest request)
	{
		var path = new NavPath();
		var start = request.StartWorld;
		var target = request.TargetWorld;
		var obstacles = _world.SpaceObstacles;

		// Line-of-sight test.
		var blocking = FindFirstBlocking(start, target, obstacles);
		if (!blocking.HasValue)
		{
			path.IsValid = true;
			path.Waypoints.Add(target);
			path.DebugInfo = "SpaceNav: LOS";
			return path;
		}

		var obstacle = blocking.Value;
		var detour = ComputeDetour(start, target, obstacle, obstacles);
		path.IsValid = true;
		path.Waypoints.Add(detour);
		path.Waypoints.Add(target);
		path.DebugInfo = "SpaceNav: detour";
		return path;
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
			// Choose smaller total distance.
			var dl = Vector2.Distance(start, left) + Vector2.Distance(left, target);
			var dr = Vector2.Distance(start, right) + Vector2.Distance(right, target);
			return dl <= dr ? left : right;
		}
		if (leftOk) return left;
		if (rightOk) return right;

		// Worst-case: still return one side.
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
