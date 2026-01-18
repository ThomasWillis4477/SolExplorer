using System.Numerics;

namespace IsoMauiEngine.Navigation;

internal static class Waypoints
{
	public static Vector2? NextWaypoint(IReadOnlyList<Vector2> points, int index)
	{
		if (points.Count == 0)
		{
			return null;
		}
		if ((uint)index >= (uint)points.Count)
		{
			return points[^1];
		}
		return points[index];
	}
}
