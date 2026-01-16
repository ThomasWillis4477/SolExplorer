using System.Numerics;

namespace IsoMauiEngine.Iso;

public static class IsoMath
{
	public const float TileWidth = 64f;
	public const float TileHeight = 32f;

	public static Vector2 GridToWorld(int gx, int gy)
	{
		var worldX = (gx - gy) * (TileWidth / 2f);
		var worldY = (gx + gy) * (TileHeight / 2f);
		return new Vector2(worldX, worldY);
	}

	public static Vector2 WorldToGrid(Vector2 world)
	{
		// Inverse of:
		// x = (gx - gy) * (w/2)
		// y = (gx + gy) * (h/2)
		var gx = (world.X / (TileWidth / 2f) + world.Y / (TileHeight / 2f)) * 0.5f;
		var gy = (world.Y / (TileHeight / 2f) - world.X / (TileWidth / 2f)) * 0.5f;
		return new Vector2(gx, gy);
	}

	public static Vector2 GridSnap(Vector2 world)
	{
		var g = WorldToGrid(world);
		var gx = (int)MathF.Round(g.X);
		var gy = (int)MathF.Round(g.Y);
		return GridToWorld(gx, gy);
	}

	public static float SortKey(Vector2 world) => world.Y;
}
