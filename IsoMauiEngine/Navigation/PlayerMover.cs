using System.Numerics;
using IsoMauiEngine.Entities;

namespace IsoMauiEngine.Navigation;

public sealed class PlayerMover
{
	private const float Speed = 65f;
	private const float ArrivalEpsilon = 2.5f;

	private readonly Player _player;
	private NavPath? _path;
	private int _index;

	public PlayerMover(Player player)
	{
		_player = player;
	}

	public NavPath? CurrentPath => _path;

	public void SetPath(NavPath? path)
	{
		_path = path;
		_index = 0;
	}

	public void Stop()
	{
		_path = null;
		_index = 0;
		_player.SetMotion(Vector2.Zero, isMoving: false);
	}

	public void Update(float dt, Func<Vector2, bool>? canMove)
	{
		if (_path is null || !_path.IsValid || _path.Waypoints.Count == 0)
		{
			_player.SetMotion(Vector2.Zero, isMoving: false);
			return;
		}

		var wp = Waypoints.NextWaypoint(_path.Waypoints, _index);
		if (!wp.HasValue)
		{
			Stop();
			return;
		}

		var target = wp.Value;
		var to = target - _player.WorldPos;
		var dist = to.Length();
		if (dist <= ArrivalEpsilon)
		{
			_index++;
			if (_index >= _path.Waypoints.Count)
			{
				Stop();
			}
			return;
		}

		var dir = dist > 1e-5f ? (to / dist) : Vector2.Zero;
		var step = dir * (Speed * dt);
		if (step.LengthSquared() > dist * dist)
		{
			step = to;
		}
		var next = _player.WorldPos + step;
		if (canMove?.Invoke(next) ?? true)
		{
			_player.WorldPos = next;
			_player.SetMotion(step / MathF.Max(dt, 1e-5f), isMoving: true);
		}
		else
		{
			// Blocked: stop for now (GridNav will be recomputed later when dynamic blockers exist).
			Stop();
		}
	}
}
