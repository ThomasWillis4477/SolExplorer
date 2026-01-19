using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.Entities;

public sealed class Player : Entity
{
	public bool IsSuitEquipped { get; set; }

	private const float WalkFps = 3.5f;
	private const int WalkFrames = 4;

	private Direction8 _facing = Direction8.S;
	private float _animSeconds;
	private bool _isMoving;
	private Vector2 _worldVelocity;

	public void SetMotion(Vector2 worldVelocity, bool isMoving)
	{
		_worldVelocity = worldVelocity;
		_isMoving = isMoving;
		if (!isMoving)
		{
			return;
		}

		// Convert world direction back to "move-space" used by FacingFromVector.
		// Inverse of: world = (mx - my, 0.5*(mx + my))
		var dx = worldVelocity.X;
		var dy = worldVelocity.Y;
		var mx = dy + dx * 0.5f;
		var my = dy - dx * 0.5f;
		_facing = FacingFromVector(new Vector2(mx, my));
	}

	public override void Update(float dt, InputState input)
	{
		if (!_isMoving)
		{
			_animSeconds = 0f;
			return;
		}

		_animSeconds += dt;
	}

	public override void EmitDrawItems(List<DrawItem> drawItems)
	{
		drawItems.Add(CreateDrawItem());
	}

	internal DrawItem CreateDrawItem()
	{
		var useSuitSprite = IsSuitEquipped;
		var frame = useSuitSprite ? 0 : (_isMoving ? (int)(_animSeconds * WalkFps) % WalkFrames : 0);
		var moving = useSuitSprite ? false : _isMoving;
		return new DrawItem(
			DrawItemType.Player,
			WorldPos,
			IsoMath.SortKey(WorldPos) + 0.001f,
			_facing,
			Frame: frame,
			IsMoving: moving,
			LayerBias: 0f,
			Kind: DrawKind.Entity,
			IsSuitEquipped: IsSuitEquipped);
	}

	private static Direction8 FacingFromVector(Vector2 move)
	{
		var x = move.X;
		var y = move.Y;

		if (x == 0 && y < 0) return Direction8.N;
		if (x > 0 && y < 0) return Direction8.NE;
		if (x > 0 && y == 0) return Direction8.E;
		if (x > 0 && y > 0) return Direction8.SE;
		if (x == 0 && y > 0) return Direction8.S;
		if (x < 0 && y > 0) return Direction8.SW;
		if (x < 0 && y == 0) return Direction8.W;
		return Direction8.NW;
	}
}
