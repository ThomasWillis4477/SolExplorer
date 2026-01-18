using System.Numerics;

namespace IsoMauiEngine.Navigation;

public enum RequesterType
{
	Player,
	Module
}

public enum MovementMode
{
	InsideModule,
	EVA,
	ModuleRCS
}

public readonly record struct NavRequest(
	RequesterType RequesterType,
	Vector2 StartWorld,
	Vector2 TargetWorld);

public sealed class NavPath
{
	public List<Vector2> Waypoints { get; } = new();
	public bool IsValid { get; set; }
	public string DebugInfo { get; set; } = string.Empty;
}
