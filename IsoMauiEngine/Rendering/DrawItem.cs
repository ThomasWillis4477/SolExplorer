using System.Numerics;

namespace IsoMauiEngine.Rendering;

public enum DrawItemType
{
	Tile = 0,
	Player = 1,
}

public readonly record struct DrawItem(
	DrawItemType Type,
	Vector2 WorldPos,
	float SortY,
	Direction8 Facing,
	int Frame,
	bool IsMoving
);

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
