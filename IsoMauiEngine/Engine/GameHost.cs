using System.Diagnostics;
using System.Numerics;
using IsoMauiEngine.Navigation;
using IsoMauiEngine.Rendering;
using IsoMauiEngine.World;
using Microsoft.Maui.Dispatching;

namespace IsoMauiEngine.Engine;

public sealed class GameHost
{
	private readonly IDispatcher _dispatcher;
	private readonly Stopwatch _stopwatch = new();
	private long _lastTicks;
	private IDispatcherTimer? _timer;
	private GraphicsView? _view;

	public GameHost(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;

		Clock = new GameClock(1f / 60f);
		Input = new InputState();
		Camera = new Camera2D();
		Renderer = new Renderer2D(Camera);

		World = new GameWorld();
		Navigation = new NavigationManager(World, Camera);
		FocusOnPlayer();
	}

	public GameWorld World { get; }
	public InputState Input { get; }
	public Camera2D Camera { get; }
	public Renderer2D Renderer { get; }
	public GameClock Clock { get; }
	public NavigationManager Navigation { get; }

	/// <summary>
	/// Provides the world-space point the camera should be centered on.
	/// Swap this to follow another object later (e.g., NPC, projectile, free-cam anchor).
	/// </summary>
	public Func<Vector2>? CameraFocusProvider { get; private set; }

	public void FocusOn(Func<Vector2> focusProvider)
	{
		CameraFocusProvider = focusProvider ?? throw new ArgumentNullException(nameof(focusProvider));
	}

	public void FocusOnPlayer()
	{
		CameraFocusProvider = () => World.Player.WorldPos;
	}

	public void Start(GraphicsView view)
	{
		_view = view;
		Stop();

		_stopwatch.Restart();
		_lastTicks = _stopwatch.ElapsedTicks;

		_timer = _dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromMilliseconds(16);
		_timer.IsRepeating = true;
		_timer.Tick += OnTick;
		_timer.Start();
	}

	public void Stop()
	{
		if (_timer is not null)
		{
			_timer.Tick -= OnTick;
			_timer.Stop();
			_timer = null;
		}
	}

	private void OnTick(object? sender, EventArgs e)
	{
		var nowTicks = _stopwatch.ElapsedTicks;
		var frameSeconds = (float)((nowTicks - _lastTicks) / (double)Stopwatch.Frequency);
		_lastTicks = nowTicks;

		var steps = Clock.Step(frameSeconds);
		for (var i = 0; i < steps; i++)
		{
			Update(Clock.DeltaTime);
		}

		_view?.Invalidate();
	}

	private void Update(float dt)
	{
		Navigation.Update(dt);
		World.Update(dt, Input);
		UpdateCamera();
	}

	private void UpdateCamera()
	{
		var focus = CameraFocusProvider?.Invoke() ?? Vector2.Zero;
		Camera.Position = focus;
	}
}
