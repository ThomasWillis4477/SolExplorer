using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.Entities;

public sealed class Player : Entity
{
	public bool IsSuitEquipped { get; set; }
	public bool IsInRcsMode => _isInRcsMode;

	private const float WalkFps = 3.5f;
	private const int WalkFrames = 4;

	private Direction8 _facing = Direction8.S;
	private float _animSeconds;
	private bool _isMoving;
	private Vector2 _worldVelocity;
	private bool _isInRcsMode;
	private bool _prevSuitEquippedForRcs;

	public void EnterRcsMode()
	{
		if (_isInRcsMode)
		{
			return;
		}
		_isInRcsMode = true;
		_prevSuitEquippedForRcs = IsSuitEquipped;
	}

	public void ExitRcsMode()
	{
		if (!_isInRcsMode)
		{
			return;
		}
		_isInRcsMode = false;
		// Restore the previously-active player presentation (engineer vs spacesuit).
		IsSuitEquipped = _prevSuitEquippedForRcs;
	}

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
		if (_isInRcsMode)
		{
			// When seated at the console, the player renders as an RCS console variant.
			// Variant mapping:
			// - Engineer sprite -> RCSconsole1
			// - Spacesuit sprite -> RCSconsole2
			var variant = _prevSuitEquippedForRcs ? 2 : 1;
			return new DrawItem(
				DrawItemType.Player,
				WorldPos,
				IsoMath.SortKey(WorldPos) + 0.001f,
				Facing: Direction8.S,
				Frame: variant,
				IsMoving: false,
				LayerBias: 0f,
				Kind: DrawKind.RcsConsolePlayer,
				IsSuitEquipped: _prevSuitEquippedForRcs);
		}

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
