using System.Numerics;

namespace IsoMauiEngine.Rendering;

public enum DrawItemType
{
	Tile = 0,
	Player = 1,
}

public enum DrawKind
{
	FloorTile,
	WallTile,
	DoorTile,
	Entity,
	Marker,
}

public readonly record struct DrawItem(
	DrawItemType Type,
	Vector2 WorldPos,
	float SortY,
	Direction8 Facing,
	int Frame,
	bool IsMoving,
	float HeightBias = 0f,
	float LayerBias = 0f,
	float Height = 0f,
	DrawKind Kind = DrawKind.Entity
)
{
	/// <summary>
	/// Sorting key used by rendering passes.
	///
	/// - Base is <c>SortY</c> (typically the entity "feet" world Y).
	/// - <c>LayerBias</c> provides explicit layering (tiles &lt; props &lt; entities &lt; UI).
	/// - <c>HeightBias</c> adjusts ordering for tall sprites/buildings without changing world position.
	///   Convention: negative values sort earlier (draw behind); positive values sort later (draw in front).
	/// </summary>
	public float SortKey
	{
		get
		{
			// Walls/doors are raised blocks, but we still sort by the base "feet" (SortY).
			// A tiny automatic negative bias helps tall blocks draw slightly earlier and reduces popping.
			var autoHeightBias = (Kind is DrawKind.WallTile or DrawKind.DoorTile)
				? (-0.001f * Height)
				: 0f;
			return SortY + HeightBias + LayerBias + autoHeightBias;
		}
	}
}

public enum Direction8
{
	N,
	NE,
	E,
	SE,
	S,
	SW,
	W,
	NW,
}
