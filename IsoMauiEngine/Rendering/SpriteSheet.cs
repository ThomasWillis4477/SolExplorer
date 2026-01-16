namespace IsoMauiEngine.Rendering;

public sealed class SpriteSheet
{
	public SpriteSheet(Microsoft.Maui.Graphics.IImage image, int columns, int rows)
	{
		Image = image ?? throw new ArgumentNullException(nameof(image));
		Columns = columns;
		Rows = rows;

		if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
		if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
	}

	public Microsoft.Maui.Graphics.IImage Image { get; }
	public int Columns { get; }
	public int Rows { get; }

	public Microsoft.Maui.Graphics.RectF GetSourceRect(int column, int row)
	{
		column = Math.Clamp(column, 0, Columns - 1);
		row = Math.Clamp(row, 0, Rows - 1);

		var cellW = (float)Image.Width / Columns;
		var cellH = (float)Image.Height / Rows;

		return new Microsoft.Maui.Graphics.RectF(column * cellW, row * cellH, cellW, cellH);
	}
}
