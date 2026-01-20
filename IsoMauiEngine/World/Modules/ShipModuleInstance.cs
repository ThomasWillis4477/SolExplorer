using System.Numerics;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Navigation;

namespace IsoMauiEngine.World.Modules;

public sealed class ShipModuleInstance
{
	private static int NextId;
	private readonly ModuleCell[,] _cells;

	public ShipModuleInstance(ModuleBlueprint blueprint, ModuleSizePreset sizePreset, int originX, int originY)
	{
		ModuleId = Interlocked.Increment(ref NextId);
		Blueprint = blueprint;
		SizePreset = sizePreset;
		OriginX = originX;
		OriginY = originY;

		Width = blueprint.Width;
		Height = blueprint.Height;

		_cells = new ModuleCell[Width, Height];
		GenerateCells();
	}

	public int ModuleId { get; }
	public ModuleSizePreset SizePreset { get; }

	public ModuleBlueprint Blueprint { get; }
	public int OriginX { get; }
	public int OriginY { get; }

	// World-space translation applied to every tile in the module (for RCS movement).
	public Vector2 WorldOffset { get; set; }

	public int Width { get; }
	public int Height { get; }

	public bool IsDerelict { get; set; }
	public bool IsAirlock { get; set; }
	public bool IsCommandModule { get; set; }

	public bool TryGetDoorSideAtWorldCell(int worldGridX, int worldGridY, out DoorSide side)
	{
		var n = GetDoorWorldCell(DoorSide.North);
		if (n.x == worldGridX && n.y == worldGridY)
		{
			side = DoorSide.North;
			return true;
		}
		var s = GetDoorWorldCell(DoorSide.South);
		if (s.x == worldGridX && s.y == worldGridY)
		{
			side = DoorSide.South;
			return true;
		}
		var w = GetDoorWorldCell(DoorSide.West);
		if (w.x == worldGridX && w.y == worldGridY)
		{
			side = DoorSide.West;
			return true;
		}
		var e = GetDoorWorldCell(DoorSide.East);
		if (e.x == worldGridX && e.y == worldGridY)
		{
			side = DoorSide.East;
			return true;
		}

		side = DoorSide.North;
		return false;
	}

	public (int x, int y) GetDoorWorldCell(DoorSide side)
	{
		return side switch
		{
			DoorSide.North => WorldGridOf(Blueprint.NorthDoor),
			DoorSide.South => WorldGridOf(Blueprint.SouthDoor),
			DoorSide.West => WorldGridOf(Blueprint.WestDoor),
			DoorSide.East => WorldGridOf(Blueprint.EastDoor),
			_ => WorldGridOf(Blueprint.NorthDoor)
		};
	}

	public Vector2 GetDoorWorldPos(DoorSide side)
	{
		var c = GetDoorWorldCell(side);
		return IsoMath.GridToWorld(c.x, c.y) + WorldOffset;
	}

	public static Vector2 GetWorldStepForSide(DoorSide side)
	{
		var o = IsoMath.GridToWorld(0, 0);
		var stepX = IsoMath.GridToWorld(1, 0) - o;
		var stepY = IsoMath.GridToWorld(0, 1) - o;
		return side switch
		{
			DoorSide.North => -stepY,
			DoorSide.South => stepY,
			DoorSide.East => stepX,
			DoorSide.West => -stepX,
			_ => stepX
		};
	}

	public static float GetExpectedDoorSeamDistance(DoorSide sideA)
	{
		return GetWorldStepForSide(sideA).Length();
	}

	public Vector2 GetWorldCenter()
	{
		var gx = OriginX + (Width / 2);
		var gy = OriginY + (Height / 2);
		return IsoMath.GridToWorld(gx, gy) + WorldOffset;
	}

	public bool TryGetCell(int worldGridX, int worldGridY, out ModuleCell cell)
	{
		var x = worldGridX - OriginX;
		var y = worldGridY - OriginY;
		if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
		{
			cell = default;
			return false;
		}

		cell = _cells[x, y];
		return true;
	}

	public (int x, int y) WorldGridOf(Vector2 localGrid)
	{
		return ((int)localGrid.X + OriginX, (int)localGrid.Y + OriginY);
	}

	private void GenerateCells()
	{
		// Defaults: floor everywhere.
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				_cells[x, y] = new ModuleCell(CellKind.Floor, Walkable: true, Height: 0f);
			}
		}

		// Perimeter walls.
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				if (Blueprint.IsWallCell(x, y))
				{
					_cells[x, y] = new ModuleCell(CellKind.Wall, Walkable: false, Height: 1f);
				}
			}
		}

		// Doors (walkable for now).
		foreach (var (dx, dy) in Blueprint.EnumerateDoorCells())
		{
			if ((uint)dx < (uint)Width && (uint)dy < (uint)Height)
			{
				_cells[dx, dy] = new ModuleCell(CellKind.Door, Walkable: true, Height: 1f);
			}
		}

		// RCS control marker.
		var rcsX = (int)Blueprint.RcsControl.X;
		var rcsY = (int)Blueprint.RcsControl.Y;
		if ((uint)rcsX < (uint)Width && (uint)rcsY < (uint)Height)
		{
			_cells[rcsX, rcsY] = new ModuleCell(CellKind.RcsControl, Walkable: true, Height: 0f);
		}

		// Locker marker.
		var lockerX = (int)Blueprint.Locker.X;
		var lockerY = (int)Blueprint.Locker.Y;
		if ((uint)lockerX < (uint)Width && (uint)lockerY < (uint)Height)
		{
			_cells[lockerX, lockerY] = new ModuleCell(CellKind.Locker, Walkable: true, Height: 0f);
		}
	}
}
