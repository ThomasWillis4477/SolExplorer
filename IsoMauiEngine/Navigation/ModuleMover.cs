using System.Numerics;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.Navigation;

public sealed class ModuleMover
{
	private const float Speed = 75f;
	private const float ArrivalEpsilon = 3.0f;

	private ShipModuleInstance? _module;
	private NavPath? _path;
	private int _index;

	public ShipModuleInstance? ActiveModule => _module;
	public NavPath? CurrentPath => _path;

	public void SetActiveModule(ShipModuleInstance? module)
	{
		_module = module;
		_path = null;
		_index = 0;
	}

	public void SetPath(NavPath? path)
	{
		_path = path;
		_index = 0;
	}

	public void Stop()
	{
		_path = null;
		_index = 0;
	}

	public void Update(float dt)
	{
		if (_module is null || _path is null || !_path.IsValid || _path.Waypoints.Count == 0)
		{
			return;
		}

		var wp = Waypoints.NextWaypoint(_path.Waypoints, _index);
		if (!wp.HasValue)
		{
			Stop();
			return;
		}
		var target = wp.Value;
		var center = _module.GetWorldCenter();
		var to = target - center;
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
		_module.WorldOffset += step;
	}
}
