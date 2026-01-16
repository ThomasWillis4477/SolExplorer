using System.Numerics;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.World;

public sealed class TileMap
{
	private readonly List<DrawItem> _tileDrawItems;

	public TileMap(int width, int height)
	{
		Width = width;
		Height = height;

		Tiles = new int[width, height];

		_tileDrawItems = new List<DrawItem>(width * height);
		RebuildDrawCache();
	}

	public int Width { get; }
	public int Height { get; }
	public int[,] Tiles { get; }

	private void RebuildDrawCache()
	{
		_tileDrawItems.Clear();
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				var worldPos = IsoMath.GridToWorld(x, y);
				_tileDrawItems.Add(new DrawItem(
					DrawItemType.Tile,
					worldPos,
					IsoMath.SortKey(worldPos),
					Facing: Direction8.S,
					Frame: 0,
					IsMoving: false));
			}
		}
	}

	public void AppendDrawItems(List<DrawItem> drawItems)
	{
		drawItems.AddRange(_tileDrawItems);
	}
}
