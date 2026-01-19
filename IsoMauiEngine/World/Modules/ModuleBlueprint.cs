using System.Numerics;

namespace IsoMauiEngine.World.Modules;

public sealed class ModuleBlueprint
{
	public ModuleBlueprint(int width, int height)
	{
		Width = width;
		Height = height;

		// One door centered on each wall.
		NorthDoor = new Vector2(width / 2, 0);
		SouthDoor = new Vector2(width / 2, height - 1);
		WestDoor = new Vector2(0, height / 2);
		EastDoor = new Vector2(width - 1, height / 2);

		// Simple default marker position.
		RcsControl = new Vector2(2 ,2);
		Locker = new Vector2(-1, -1);
	}

	public int Width { get; }
	public int Height { get; }

	public Vector2 NorthDoor { get; }
	public Vector2 SouthDoor { get; }
	public Vector2 WestDoor { get; }
	public Vector2 EastDoor { get; }

	public Vector2 RcsControl { get; set; }
	public Vector2 Locker { get; set; }

	public bool IsWallCell(int x, int y)
	{
		return x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
	}

	public IEnumerable<(int x, int y)> EnumerateDoorCells()
	{
		yield return ((int)NorthDoor.X, (int)NorthDoor.Y);
		yield return ((int)SouthDoor.X, (int)SouthDoor.Y);
		yield return ((int)WestDoor.X, (int)WestDoor.Y);
		yield return ((int)EastDoor.X, (int)EastDoor.Y);
	}
}
