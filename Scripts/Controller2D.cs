using Godot;
using Godot.Collections;
using System;
using System.Threading;
using static Godot.GD;

// [Tool]
public partial class Controller2D : Node2D
{
	private Cell2D[,] Cells;

	[ExportCategory("CellsSettings")]
	// [Export] private int Length = 100;
	// [Export] private int Width = 100;
	[Export] private int CellResolution = 100;
	[Export] private float Conductivity = 0.1f;

	[ExportCategory("TextureSettings")]
	[Export] private PackedScene ChunkPrefab;
	private TextureRect TextureRect;
	[Export] private float CellSize = 1f;
	private Image Image;

	[Export] private Godot.Timer Timer;

	[ExportCategory("HeatSettings")]
	[ExportGroup("ColorSettings")]
	[Export] private Color ColdColor = new(0, 0, 1);
	[Export] private Color HotColor = new(1, 0, 0);
	[ExportGroup("TempSettings")]
	[Export] private float ColdThreshold = -100;
	[Export] private float ZeroThreshold = 0;
	[Export] private float HotThreshold = 100;

	public override void _Ready()
	{
		TextureRect = ChunkPrefab.Instantiate<TextureRect>();
		AddChild(TextureRect);

		Cells = CreateCells(CellResolution, CellResolution);
		ChangeColor();

		Timer.Timeout += Calculate;
		Timer.Timeout += ChangeColor;
	}

	public override void _PhysicsProcess(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.R)
			{
				Cells = CreateCells(CellResolution, CellResolution);
				ChangeColor();
			}
		}
	}

	private Color GetHeatColor_H(float temperature)
	{
		if (temperature < ColdThreshold)
			return ColdColor;
		else if (temperature > HotThreshold)
			return HotColor;
		else
		{
			float range = Mathf.Abs(HotThreshold - ColdThreshold);
			float t = Mathf.Abs(temperature - ColdThreshold) / range * (240f / 359f);
			Color color = new(1, 0, 0);
			color.H = t;
			return color;
		}
	}

	private Color GetHeatColor_S(float temperature)
	{
		if (temperature < ColdThreshold)
			return ColdColor;
		else if (temperature < ZeroThreshold)
		{
			float t = Mathf.Abs(temperature - ColdThreshold) / Mathf.Abs(ZeroThreshold - ColdThreshold);
			Color color = ColdColor;
			color.S = 1 - t;
			return color;
		}
		else if (temperature == ZeroThreshold)
			return Colors.White;
		else if (temperature < HotThreshold)
		{
			float t = Mathf.Abs(temperature - HotThreshold) / Mathf.Abs(ZeroThreshold - HotThreshold);
			Color color = HotColor;
			color.S = 1 - t;
			return color;
		}
		else
			return HotColor;
	}

	private static Cell2D[,] CreateCells(int length, int width)
	{
		Cell2D[,] cells = new Cell2D[length, width];
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < width; j++)
			{
				cells[i, j] = new Cell2D(RandRange(-100, 100), new Vector3(i, j, 0));
			}
		}
		return cells;
	}

	private void Calculate()
	{
		int Length = Cells.GetLength(0);
		int Width = Cells.GetLength(1);

		for (int i = 0; i < Length; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				float localT = Cells[i, j].Temperature;
				float deltaT = 0;
				if (i + 1 < Length)
					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
				else
					deltaT += (Cells[0, j].Temperature - localT) * Conductivity;
				if (i - 1 >= 0)
					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
				else
					deltaT += (Cells[Length - 1, j].Temperature - localT) * Conductivity;
				if (j + 1 < Width)
					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
				else
					deltaT += (Cells[i, 0].Temperature - localT) * Conductivity;
				if (j - 1 >= 0)
					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
				else
					deltaT += (Cells[i, Width - 1].Temperature - localT) * Conductivity;

				Cells[i, j].Temperature += deltaT;
			}
		}
	}

	private void ChangeColor()
	{
		int Length = Cells.GetLength(0);
		int Width = Cells.GetLength(1);

		if (Image is null)
		{
			TextureRect.Size = new Vector2(Length * CellSize, Width * CellSize);
			Image = Image.CreateEmpty(Length, Width, false, Image.Format.Rgb8);
		}
		for (int i = 0; i < Length; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				Color color = GetHeatColor_S(Cells[i, j].Temperature);
				Image.SetPixel(i, j, color);
			}
		}
		TextureRect.Texture = ImageTexture.CreateFromImage(Image);
	}

}



public class Cell2D
{
	public float Temperature;
	// public Vector2 GeoCoordinate;
	public Vector3 LocalPosition;

	public Cell2D(float _temperature, Vector3 _localPosition)
	{
		Temperature = _temperature;
		LocalPosition = _localPosition;
	}
}

public class Cube
{
	public Vector3 toward;
	public Vector3 towardMask;
	public Cell2D[,] Cells;
	public int CellResolution;

	public Cube(Vector3 _toward, Vector3 _towardMask, int _cellResolution)
	{
		toward = _toward;
		towardMask = _towardMask;
		CellResolution = _cellResolution;
		Cells = new Cell2D[CellResolution, CellResolution];
		GenerateCells();
	}

	public void GenerateCells()
	{
		int radius = CellResolution / 2;
		for (int i = 0; i < CellResolution; i++)
		{
			for (int j = 0; j < CellResolution; j++)
			{
				Vector3 innerPos;
				Vector3 localPos = toward * radius;
				if (towardMask == Toward.UpMask)
					innerPos = new Vector3(i - radius, 0, j - radius);
				else if (towardMask == Toward.DownMask)
					innerPos = new Vector3(i - radius, 0, j - radius);
				else if (towardMask == Toward.LeftMask)
					innerPos = new Vector3(0, i - radius, j - radius);
				else if (towardMask == Toward.RightMask)
					innerPos = new Vector3(0, i - radius, j - radius);
				else if (towardMask == Toward.ForwardMask)
					innerPos = new Vector3(i - radius, j - radius, 0);
				else if (towardMask == Toward.BackMask)
					innerPos = new Vector3(i - radius, j - radius, 0);
				else
				{
					innerPos = Vector3.Zero;
					Print("CubeGene:wtf你为什么没输入一个合理的遮罩");
				}
				localPos += innerPos;
				Cells[i, j] = new Cell2D(RandRange(-100, 100), localPos);
			}
		}
	}
}

public struct Toward
{
	public static readonly Vector3 Up = Vector3.Up;
	public static readonly Vector3 UpMask = new(1, 0, 1);

	public static readonly Vector3 Down = Vector3.Down;
	public static readonly Vector3 DownMask = new(-1, 0, 1);

	public static readonly Vector3 Left = Vector3.Left;
	public static readonly Vector3 LeftMask = new(0, 1, 1);

	public static readonly Vector3 Right = Vector3.Right;
	public static readonly Vector3 RightMask = new(0, -1, 1);

	public static readonly Vector3 Forward = Vector3.Forward;
	public static readonly Vector3 ForwardMask = new(-1, 1, 0);

	public static readonly Vector3 Back = Vector3.Back;
	public static readonly Vector3 BackMask = new(1, 1, 0);
}