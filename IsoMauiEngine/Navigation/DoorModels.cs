using System.Numerics;

namespace IsoMauiEngine.Navigation;

public enum DoorSide
{
	North,
	East,
	South,
	West
}

public static class DoorSideExtensions
{
	public static DoorSide Opposite(this DoorSide side)
	{
		return side switch
		{
			DoorSide.North => DoorSide.South,
			DoorSide.South => DoorSide.North,
			DoorSide.East => DoorSide.West,
			DoorSide.West => DoorSide.East,
			_ => DoorSide.North
		};
	}

	public static bool IsOppositeTo(this DoorSide a, DoorSide b)
	{
		return a.Opposite() == b;
	}
}

public readonly record struct Door(
	DoorSide Side,
	Vector2 LocalTilePos,
	Vector2 WorldPos,
	bool IsIntact,
	bool IsSealed);
