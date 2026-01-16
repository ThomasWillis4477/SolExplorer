using Stopwatch = System.Diagnostics.Stopwatch;
using IsoMauiEngine.Rendering;
using Microsoft.Maui.Graphics;

namespace IsoMauiEngine.Views;

public sealed class AnimationTestPage : ContentPage
{
	private readonly GraphicsView _graphicsView;
	private readonly SpriteTestDrawable _drawable;
	private readonly Picker _facingPicker;
	private readonly Switch _movingSwitch;
	private readonly Switch _playingSwitch;
	private readonly Slider _fpsSlider;
	private readonly Slider _heightSlider;
	private readonly Label _statusLabel;
	private readonly IDispatcherTimer _timer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private long _lastTickMs;
	private double _frameAccumulator;
	private int _frame;
	private Direction8 _facing = Direction8.S;
	private bool _isMoving = true;
	private bool _isPlaying = true;
	private double _fps = 10;
	private double _spriteHeight = 96;

	public AnimationTestPage()
	{
		Title = "Animation Test";
		BackgroundColor = Color.FromArgb("#0B0F14");

		_drawable = new SpriteTestDrawable(() => new SpriteTestState(
			Facing: _facing,
			IsMoving: _isMoving,
			Frame: _frame,
			SpriteHeight: (float)_spriteHeight
		));

		_graphicsView = new GraphicsView
		{
			Drawable = _drawable,
			BackgroundColor = Colors.Transparent,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			MinimumHeightRequest = 320,
		};

		_facingPicker = new Picker
		{
			Title = "Facing",
			HorizontalOptions = LayoutOptions.Fill,
			TextColor = Colors.White,
		};
		foreach (var d in Enum.GetValues<Direction8>())
		{
			_facingPicker.Items.Add(d.ToString());
		}
		_facingPicker.SelectedIndex = (int)_facing;
		_facingPicker.SelectedIndexChanged += (_, __) =>
		{
			if (_facingPicker.SelectedIndex >= 0)
			{
				_facing = (Direction8)_facingPicker.SelectedIndex;
				UpdateStatus();
				_graphicsView.Invalidate();
			}
		};

		_movingSwitch = new Switch { IsToggled = _isMoving, HorizontalOptions = LayoutOptions.End };
		_movingSwitch.Toggled += (_, e) =>
		{
			_isMoving = e.Value;
			UpdateStatus();
			_graphicsView.Invalidate();
		};

		_playingSwitch = new Switch { IsToggled = _isPlaying, HorizontalOptions = LayoutOptions.End };
		_playingSwitch.Toggled += (_, e) =>
		{
			_isPlaying = e.Value;
			UpdateStatus();
		};

		_fpsSlider = new Slider { Minimum = 1, Maximum = 24, Value = _fps, HorizontalOptions = LayoutOptions.Fill };
		_fpsSlider.ValueChanged += (_, e) =>
		{
			_fps = Math.Max(1, e.NewValue);
			UpdateStatus();
		};

		_heightSlider = new Slider { Minimum = 32, Maximum = 192, Value = _spriteHeight, HorizontalOptions = LayoutOptions.Fill };
		_heightSlider.ValueChanged += (_, e) =>
		{
			_spriteHeight = e.NewValue;
			UpdateStatus();
			_graphicsView.Invalidate();
		};

		var prevButton = new Button { Text = "◀ Prev", BackgroundColor = Color.FromArgb("#1B2632"), TextColor = Colors.White };
		prevButton.Clicked += (_, __) =>
		{
			_frame = Math.Max(0, _frame - 1);
			UpdateStatus();
			_graphicsView.Invalidate();
		};

		var nextButton = new Button { Text = "Next ▶", BackgroundColor = Color.FromArgb("#1B2632"), TextColor = Colors.White };
		nextButton.Clicked += (_, __) =>
		{
			_frame++;
			UpdateStatus();
			_graphicsView.Invalidate();
		};

		var resetButton = new Button { Text = "Reset", BackgroundColor = Color.FromArgb("#1B2632"), TextColor = Colors.White };
		resetButton.Clicked += (_, __) =>
		{
			_frame = 0;
			_frameAccumulator = 0;
			UpdateStatus();
			_graphicsView.Invalidate();
		};

		_statusLabel = new Label
		{
			TextColor = Colors.White,
			FontSize = 12,
			Opacity = 0.9,
			LineBreakMode = LineBreakMode.WordWrap,
		};

		var grid1 = new Grid
		{
			ColumnDefinitions = new ColumnDefinitionCollection
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star),
			},
			RowDefinitions = new RowDefinitionCollection
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
			},
		};
		grid1.Add(new Label { Text = "Facing", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
		grid1.Add(_facingPicker, 1, 0);
		grid1.Add(new Label { Text = "Animate", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 1);
		grid1.Add(_movingSwitch, 1, 1);
		grid1.Add(new Label { Text = "Playing", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 2);
		grid1.Add(_playingSwitch, 1, 2);
		grid1.Add(new Label { Text = "FPS", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 3);
		grid1.Add(_fpsSlider, 1, 3);

		var grid2 = new Grid
		{
			ColumnDefinitions = new ColumnDefinitionCollection
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star),
			},
		};
		grid2.Add(new Label { Text = "Sprite Height", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
		grid2.Add(_heightSlider, 1, 0);

		var controls = new VerticalStackLayout
		{
			Spacing = 10,
			Children =
			{
				new Label
				{
					Text = "Engineer Sprite Animation Test",
					TextColor = Colors.White,
					FontSize = 18,
					FontAttributes = FontAttributes.Bold,
				},
				grid1,
				grid2,
				new HorizontalStackLayout
				{
					Spacing = 10,
					Children = { prevButton, nextButton, resetButton }
				},
				_statusLabel,
			}
		};

		var controlsScroll = new ScrollView
		{
			Content = controls,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Start,
		};

		// Keep controls readable on wide windows.
		controls.WidthRequest = 720;
		controls.HorizontalOptions = LayoutOptions.Center;

		var root = new Grid
		{
			RowDefinitions = new RowDefinitionCollection
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			},
			Padding = 16,
			RowSpacing = 12,
		};

		root.Children.Add(controlsScroll);
		Grid.SetRow(_graphicsView, 1);
		root.Children.Add(_graphicsView);

		Content = root;

		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromMilliseconds(16);
		_timer.Tick += (_, __) => Tick();
		_timer.Start();

		UpdateStatus();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = EnsureSpritesLoadedAsync();
	}

	private async Task EnsureSpritesLoadedAsync()
	{
		await SpriteAssets.EnsureLoadedAsync();
		UpdateStatus();
		_graphicsView.Invalidate();
	}

	private void Tick()
	{
		var nowMs = _clock.ElapsedMilliseconds;
		var dtMs = nowMs - _lastTickMs;
		_lastTickMs = nowMs;

		if (dtMs <= 0 || dtMs > 250)
		{
			return;
		}

		if (!_isPlaying || !_isMoving)
		{
			return;
		}

		var fps = Math.Max(1.0, _fps);
		var framePeriod = 1.0 / fps;
		_frameAccumulator += dtMs / 1000.0;

		while (_frameAccumulator >= framePeriod)
		{
			_frame++;
			_frameAccumulator -= framePeriod;
		}

		UpdateStatus();
		_graphicsView.Invalidate();
	}

	private void UpdateStatus()
	{
		var ready = SpriteAssets.IsReady;
			var which = _isMoving ? "Walking (animated)" : "Walking (frame 0)";

		var (row, col, cols, rows) = SpriteTestDrawable.ComputeSheetCell(_facing, _frame, _isMoving);

		_statusLabel.Text =
			$"Sprites ready: {ready}\n" +
			$"Mode: {which} | Facing: {_facing} | Frame: {_frame} | Playing: {_isPlaying} | FPS: {Math.Round(_fps, 1)}\n" +
			$"Sheet cell: row={row}, col={col} (sheet {cols}x{rows}) | Draw height: {Math.Round(_spriteHeight)}px\n" +
				"Tip: Toggle Animate to freeze on frame 0.";
	}

	private readonly record struct SpriteTestState(Direction8 Facing, bool IsMoving, int Frame, float SpriteHeight);

	private sealed class SpriteTestDrawable : IDrawable
	{
		private readonly Func<SpriteTestState> _get;

		public SpriteTestDrawable(Func<SpriteTestState> get) => _get = get;

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.SaveState();
			canvas.FillColor = Color.FromArgb("#0B0F14");
			canvas.FillRectangle(dirtyRect);

			DrawGuides(canvas, dirtyRect);

			var state = _get();
			TryDrawSprite(canvas, dirtyRect, state);

			canvas.RestoreState();
		}

		public static (int Row, int Col, int Cols, int Rows) ComputeSheetCell(Direction8 facing, int frame, bool isMoving)
		{
			var walking = SpriteAssets.EngineerWalking;

				// Walking sheet: 8 direction columns x 4 frame rows.
				if (walking is not null)
				{
					var col = Math.Clamp((int)facing, 0, walking.Columns - 1);
					var row = isMoving
						? (((frame % walking.Rows) + walking.Rows) % walking.Rows)
						: 0;
					return (row, col, walking.Columns, walking.Rows);
				}

			return (0, 0, 0, 0);
		}

		private static void DrawGuides(ICanvas canvas, RectF rect)
		{
			var cx = rect.Center.X;
			var cy = rect.Center.Y;

			canvas.StrokeColor = Color.FromArgb("#223140");
			canvas.StrokeSize = 1;
			canvas.DrawLine(cx, rect.Top, cx, rect.Bottom);
			canvas.DrawLine(rect.Left, cy, rect.Right, cy);

			// "Feet" anchor line (keep it near bottom so sprite stays visible)
			var feetY = rect.Bottom - 60;
			canvas.StrokeColor = Color.FromArgb("#335066");
			canvas.DrawLine(rect.Left + 20, feetY, rect.Right - 20, feetY);
		}

		private static void TryDrawSprite(ICanvas canvas, RectF rect, SpriteTestState state)
		{
			var walking = SpriteAssets.EngineerWalking;

			SpriteSheet? sheet = null;
			int col = 0;
			int row = 0;

				if (walking is not null)
				{
					sheet = walking;
					col = Math.Clamp((int)state.Facing, 0, sheet.Columns - 1);
					row = state.IsMoving
						? (((state.Frame % sheet.Rows) + sheet.Rows) % sheet.Rows)
						: 0;
				}

			if (sheet is null)
			{
				canvas.FontColor = Colors.White;
				canvas.DrawString("(Sprites not loaded yet)", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
				return;
			}

			var src = sheet.GetSourceRect(col, row);

			// Place sprite with feet near the bottom so it doesn't clip on shorter view heights.
			var feetX = rect.Center.X;
			var feetY = rect.Bottom - 60;

			var desiredH = Math.Max(16f, state.SpriteHeight);
			var aspect = src.Width / MathF.Max(1f, src.Height);
			var desiredW = desiredH * aspect;

			var dest = new RectF(feetX - desiredW * 0.5f, feetY - desiredH, desiredW, desiredH);

			// Draw dest bounds.
			canvas.StrokeColor = Color.FromArgb("#44FFFFFF");
			canvas.StrokeSize = 1;
			canvas.DrawRectangle(dest);

			// Crop via clip + translated scaled full-sheet draw.
			var scaleX = dest.Width / MathF.Max(1f, src.Width);
			var scaleY = dest.Height / MathF.Max(1f, src.Height);
			var drawX = dest.X - src.X * scaleX;
			var drawY = dest.Y - src.Y * scaleY;
			var drawW = (float)sheet.Image.Width * scaleX;
			var drawH = (float)sheet.Image.Height * scaleY;

			canvas.SaveState();
			canvas.ClipRectangle(dest);
			canvas.DrawImage(sheet.Image, drawX, drawY, drawW, drawH);
			canvas.RestoreState();

			// Feet marker
			canvas.FillColor = Colors.Red;
			canvas.FillCircle(feetX, feetY, 3);
		}

	}
}
