using System.Numerics;

namespace IsoMauiEngine.Engine;

public sealed class Camera2D
{
	public Vector2 Position { get; set; }
	public Vector2 ScreenCenter { get; set; }

	public Vector2 WorldToScreen(Vector2 worldPos)
	{
		return ScreenCenter + (worldPos - Position);
	}
}
