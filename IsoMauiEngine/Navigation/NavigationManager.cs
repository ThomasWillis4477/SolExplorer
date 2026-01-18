using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Iso;
using IsoMauiEngine.World;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.Navigation;

public sealed class NavigationManager
{
	private readonly GameWorld _world;
	private readonly Camera2D _camera;
	private readonly GridNavigator _grid;
	private readonly SpaceNavigator _space;
	private readonly ModuleNavigator _moduleNav;
	private readonly PlayerMover _playerMover;
	private readonly ModuleMover _moduleMover;

	private int _lastGraphVersion;
	private NavRequest? _lastRequest;

	public NavigationManager(GameWorld world, Camera2D camera)
	{
		_world = world;
		_camera = camera;
		_grid = new GridNavigator(world);
		_space = new SpaceNavigator(world);
		_moduleNav = new ModuleNavigator(world);
		_playerMover = new PlayerMover(world.Player);
		_moduleMover = new ModuleMover();
		_lastGraphVersion = world.ModuleGraph.Version;
	}

	public MovementMode CurrentMode { get; private set; }
	public string ActiveNavigator { get; private set; } = string.Empty;
	public NavPath? CurrentPath => _world.RcsModeModule is not null ? _moduleMover.CurrentPath : _playerMover.CurrentPath;

	public void Update(float dt)
	{
		// Recompute if graph changed and we have an active grid path.
		if (_lastRequest.HasValue && _world.ModuleGraph.Version != _lastGraphVersion)
		{
			_lastGraphVersion = _world.ModuleGraph.Version;
			RecomputeLast();
		}

		_playerMover.Update(dt, _world.CanMoveToWorld);
		_moduleMover.Update(dt);

		// Snap docking when module is moving.
		var active = _world.RcsModeModule;
		if (active is not null && _moduleMover.CurrentPath is not null)
		{
			if (_moduleNav.TrySnapDock(active))
			{
				_moduleMover.Stop();
				_lastGraphVersion = _world.ModuleGraph.Version;
			}
		}
	}

	public void HandleLeftClickScreen(Vector2 screenPos)
	{
		var targetWorld = _camera.ScreenToWorld(screenPos);

		// Special: click an RCS control marker toggles RCS mode.
		if (_world.TryFindContainingModule(targetWorld, out var clickedModule, out var clickedCell)
			&& _world.TryGetCellKind(clickedModule, clickedCell, out var kind)
			&& kind == CellKind.RcsControl)
		{
			if (_world.RcsModeModule?.ModuleId == clickedModule.ModuleId)
			{
				_world.RcsModeModule = null;
				_moduleMover.SetActiveModule(null);
			}
			else
			{
				_world.RcsModeModule = clickedModule;
				_moduleMover.SetActiveModule(clickedModule);
			}
			return;
		}

		ResolveMode(targetWorld);
		if (CurrentMode == MovementMode.ModuleRCS && _world.RcsModeModule is not null)
		{
			ActiveNavigator = nameof(ModuleNavigator);
			var start = _world.RcsModeModule.GetWorldCenter();
			var req = new NavRequest(RequesterType.Module, start, targetWorld);
			_lastRequest = req;
			var p = _moduleNav.ComputePath(req);
			_moduleMover.SetActiveModule(_world.RcsModeModule);
			_moduleMover.SetPath(p);
			return;
		}

		// Player request
		var startWorld = _world.Player.WorldPos;
		var startInside = _world.TryFindContainingModule(startWorld, out _, out _);
		var targetInside = _world.TryFindContainingModule(targetWorld, out _, out _);

		// Hybrid transitions: module->space and space->module route via a door waypoint.
		if (startInside && !targetInside)
		{
			ActiveNavigator = "Grid+Space";
			var toDoor = _grid.ComputeExitToNearestDoor(startWorld);
			if (!toDoor.IsValid || toDoor.Waypoints.Count == 0)
			{
				_playerMover.SetPath(toDoor);
				return;
			}
			var doorPos = toDoor.Waypoints[^1];
			var spaceReq = new NavRequest(RequesterType.Player, doorPos, targetWorld);
			var space = _space.ComputePath(spaceReq);
			var combined = CombinePaths(toDoor, space, debug: "Grid+Space");
			_lastRequest = new NavRequest(RequesterType.Player, startWorld, targetWorld);
			_playerMover.SetPath(combined);
			return;
		}

		if (!startInside && targetInside)
		{
			ActiveNavigator = "Space+Grid";
			var toDoor = _grid.ComputeEntryFromNearestDoor(targetWorld);
			if (!toDoor.IsValid || toDoor.Waypoints.Count == 0)
			{
				_playerMover.SetPath(toDoor);
				return;
			}
			var doorPos = toDoor.Waypoints[0];
			var spaceReq = new NavRequest(RequesterType.Player, startWorld, doorPos);
			var space = _space.ComputePath(spaceReq);
			var combined = CombinePaths(space, toDoor, debug: "Space+Grid");
			_lastRequest = new NavRequest(RequesterType.Player, startWorld, targetWorld);
			_playerMover.SetPath(combined);
			return;
		}

		ActiveNavigator = CurrentMode == MovementMode.InsideModule ? nameof(GridNavigator) : nameof(SpaceNavigator);
		var reqPlayer = new NavRequest(RequesterType.Player, startWorld, targetWorld);
		_lastRequest = reqPlayer;
		var path = CurrentMode == MovementMode.InsideModule
			? _grid.ComputePath(reqPlayer)
			: _space.ComputePath(reqPlayer);
		_playerMover.SetPath(path);
	}

	private void ResolveMode(Vector2 targetWorld)
	{
		if (_world.RcsModeModule is not null)
		{
			CurrentMode = MovementMode.ModuleRCS;
			return;
		}
		if (_world.TryFindContainingModule(_world.Player.WorldPos, out _, out _) && _world.TryFindContainingModule(targetWorld, out _, out _))
		{
			CurrentMode = MovementMode.InsideModule;
			return;
		}
		CurrentMode = MovementMode.EVA;
	}

	private static NavPath CombinePaths(NavPath a, NavPath b, string debug)
	{
		var combined = new NavPath { IsValid = a.IsValid && b.IsValid, DebugInfo = debug };
		if (!combined.IsValid)
		{
			return combined;
		}
		for (var i = 0; i < a.Waypoints.Count; i++)
		{
			combined.Waypoints.Add(a.Waypoints[i]);
		}
		for (var i = 0; i < b.Waypoints.Count; i++)
		{
			// Avoid duplicating the stitch point.
			if (combined.Waypoints.Count > 0 && Vector2.DistanceSquared(combined.Waypoints[^1], b.Waypoints[i]) < 0.0001f)
			{
				continue;
			}
			combined.Waypoints.Add(b.Waypoints[i]);
		}
		return combined;
	}

	private void RecomputeLast()
	{
		if (!_lastRequest.HasValue)
		{
			return;
		}
		// Only auto-recompute for player grid paths (future: also if EVA obstacles change).
		ResolveMode(_lastRequest.Value.TargetWorld);
		if (CurrentMode != MovementMode.InsideModule)
		{
			return;
		}
		var req = _lastRequest.Value;
		var path = _grid.ComputePath(req);
		_playerMover.SetPath(path);
	}
}
