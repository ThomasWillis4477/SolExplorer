using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.World.Modules;

public sealed class ModuleGridMap
{
	private readonly ShipModuleInstance _module;
	private readonly List<DrawItem> _cached;
	private System.Numerics.Vector2 _cachedOffset;

	public ModuleGridMap(ShipModuleInstance module)
	{
		_module = module;
		_cached = new List<DrawItem>(module.Width * module.Height);
		RebuildDrawCache();
	}

	public ShipModuleInstance Module => _module;

	public void AppendDrawItems(List<DrawItem> drawItems)
	{
		if (_cachedOffset != _module.WorldOffset)
		{
			RebuildDrawCache();
		}
		drawItems.AddRange(_cached);
	}

	public bool IsWalkableCell(int worldGridX, int worldGridY)
	{
		return _module.TryGetCell(worldGridX, worldGridY, out var cell) && cell.Walkable;
	}

	public bool TryGetCellKind(int worldGridX, int worldGridY, out CellKind kind)
	{
		if (_module.TryGetCell(worldGridX, worldGridY, out var cell))
		{
			kind = cell.Kind;
			return true;
		}
		kind = default;
		return false;
	}

	private void RebuildDrawCache()
	{
		_cached.Clear();
		_cachedOffset = _module.WorldOffset;
		for (var y = 0; y < _module.Height; y++)
		{
			for (var x = 0; x < _module.Width; x++)
			{
				var worldGridX = _module.OriginX + x;
				var worldGridY = _module.OriginY + y;
				var worldPos = IsoMath.GridToWorld(worldGridX, worldGridY) + _cachedOffset;

				if (!_module.TryGetCell(worldGridX, worldGridY, out var cell))
				{
					continue;
				}

				switch (cell.Kind)
				{
					case CellKind.Floor:
						_cached.Add(new DrawItem(
							Type: DrawItemType.Tile,
							WorldPos: worldPos,
							SortY: IsoMath.SortKey(worldPos),
							Facing: Direction8.S,
							Frame: 0,
							IsMoving: false,
							LayerBias: -1000f,
							Height: 0f,
							Kind: DrawKind.FloorTile));
						break;

					case CellKind.Wall:
						_cached.Add(new DrawItem(
							Type: DrawItemType.Tile,
							WorldPos: worldPos,
							SortY: IsoMath.SortKey(worldPos),
							Facing: Direction8.S,
							Frame: 0,
							IsMoving: false,
							Height: cell.Height,
							Kind: DrawKind.WallTile));
						break;

					case CellKind.Door:
						_cached.Add(new DrawItem(
							Type: DrawItemType.Tile,
							WorldPos: worldPos,
							SortY: IsoMath.SortKey(worldPos),
							Facing: Direction8.S,
							Frame: 0,
							IsMoving: false,
							Height: cell.Height,
							Kind: DrawKind.DoorTile));
						break;

					case CellKind.RcsControl:
						_cached.Add(new DrawItem(
							Type: DrawItemType.Tile,
							WorldPos: worldPos,
							SortY: IsoMath.SortKey(worldPos),
							Facing: Direction8.S,
							Frame: 0,
							IsMoving: false,
							LayerBias: -0.01f,
							Height: 0f,
							Kind: DrawKind.Marker));
						break;
				}
			}
		}
	}
}
