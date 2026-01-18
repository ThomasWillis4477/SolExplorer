using System.Numerics;

namespace IsoMauiEngine.Navigation;

public readonly record struct CircleObstacle(Vector2 Center, float Radius);

internal static class ObstacleMath
{
	public static bool SegmentIntersectsCircle(Vector2 a, Vector2 b, CircleObstacle c)
	{
		// Project center onto segment and check distance.
		var ab = b - a;
		var abLenSq = ab.LengthSquared();
		if (abLenSq < 1e-6f)
		{
			return Vector2.DistanceSquared(a, c.Center) <= c.Radius * c.Radius;
		}
		var t = Vector2.Dot(c.Center - a, ab) / abLenSq;
		t = Math.Clamp(t, 0f, 1f);
		var p = a + ab * t;
		return Vector2.DistanceSquared(p, c.Center) <= c.Radius * c.Radius;
	}
}
