using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Entities;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.World;

public sealed class GameWorld
{
	private readonly List<Entity> _entities = new();
	private int width ;
	private int height ;

	public GameWorld()
	{
		width = 10;
		height =10;
		TileMap = new TileMap(width, height);

		Player = new Player
		{
			WorldPos = IsoMath.GridToWorld(width / 2, height / 2)
		};

		_entities.Add(Player);
	}

	public TileMap TileMap { get; }
	public Player Player { get; }

	public void Update(float dt, InputState input)
	{
		for (var i = 0; i < _entities.Count; i++)
		{
			_entities[i].Update(dt, input);
		}
	}

	public void AppendDrawItems(List<DrawItem> drawItems)
	{
		TileMap.AppendDrawItems(drawItems);
		for (var i = 0; i < _entities.Count; i++)
		{
			_entities[i].EmitDrawItems(drawItems);
		}
	}
}
