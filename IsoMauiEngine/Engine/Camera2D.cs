using System.Numerics;

namespace IsoMauiEngine.Engine;

public sealed class Camera2D
{
	private const float MinZoom = 0.1f;
	private const float MaxZoom = 6f;
	private float _zoom = 1f;

	public Vector2 Position { get; set; }
	public Vector2 ScreenCenter { get; set; }

	/// <summary>
	/// Camera zoom. Applies a uniform scale after translating by -Position and before translating to ScreenCenter.
	/// </summary>
	public float Zoom
	{
		get => _zoom;
		set => _zoom = Math.Clamp(value, MinZoom, MaxZoom);
	}

	/// <summary>
	/// When enabled, snaps final screen coordinates to whole pixels to reduce jitter.
	/// </summary>
	public bool PixelSnap { get; set; } = true;

	/// <summary>
	/// World->screen transform order: translate(-Position) then scale(Zoom) then translate(ScreenCenter).
	/// </summary>
	public Matrix3x2 GetWorldToScreenMatrix()
	{
		return Matrix3x2.CreateTranslation(-Position)
			* Matrix3x2.CreateScale(Zoom)
			* Matrix3x2.CreateTranslation(ScreenCenter);
	}

	public Matrix3x2 GetScreenToWorldMatrix()
	{
		var worldToScreen = GetWorldToScreenMatrix();
		if (!Matrix3x2.Invert(worldToScreen, out var screenToWorld))
		{
			return Matrix3x2.Identity;
		}

		return screenToWorld;
	}

	public Vector2 WorldToScreen(Vector2 worldPos)
	{
		var screen = Vector2.Transform(worldPos, GetWorldToScreenMatrix());
		return PixelSnap ? Snap(screen) : screen;
	}

	public Vector2 ScreenToWorld(Vector2 screenPos)
	{
		return Vector2.Transform(screenPos, GetScreenToWorldMatrix());
	}

	private static Vector2 Snap(Vector2 v) => new(MathF.Round(v.X), MathF.Round(v.Y));
}
