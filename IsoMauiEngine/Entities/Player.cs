using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.Entities;

public sealed class Player : Entity
{
	private const float Speed = 50f;
	private const float WalkFps = 3.5f;
	private const int WalkFrames = 4;

	public Func<Vector2, bool>? CanMoveToWorld { get; set; }
	private Direction8 _facing = Direction8.S;
	private float _animSeconds;
	private bool _isMoving;

	public override void Update(float dt, InputState input)
	{
		var move = input.GetMoveVector();
		_isMoving = move != Vector2.Zero;

		if (!_isMoving)
		{
			_animSeconds = 0f;
			return;
		}

		_animSeconds += dt;

		// Convert move vector to isometric world delta per prompt.
		var iso = new Vector2(move.X - move.Y, (move.X + move.Y) * 0.5f);
		if (iso.LengthSquared() > 0.0001f)
		{
			iso = Vector2.Normalize(iso);
		}

		var next = WorldPos + iso * (Speed * dt);
		if (CanMoveToWorld?.Invoke(next) ?? true)
		{
			WorldPos = next;
		}
		else
		{
			_isMoving = false;
		}
		_facing = FacingFromVector(move);
	}

	public override void EmitDrawItems(List<DrawItem> drawItems)
	{
		var frame = _isMoving ? (int)(_animSeconds * WalkFps) % WalkFrames : 0;
		drawItems.Add(new DrawItem(
			DrawItemType.Player,
			WorldPos,
			IsoMath.SortKey(WorldPos) + 0.001f,
			_facing,
			Frame: frame,
			IsMoving: _isMoving,
			LayerBias: 0f,
			Kind: DrawKind.Entity));
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
