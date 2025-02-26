using Godot;
using System;
using System.Collections.Generic;
using static Godot.GD;

// [Tool]
public partial class Controller2D : Node2D
{
	// private Cell2D[,] Cells;
	private Dictionary<string, Chunk> Chunks = new();

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
		// AddChild(TextureRect);

		// Cells = CreateCells(CellResolution, CellResolution);
		// ChangeColor();

		// Timer.Timeout += Calculate;
		// Timer.Timeout += ChangeColor;

		ChunkReady();
		TextureReady();

		Timer.Timeout += Calculate;
		Timer.Timeout += TextureUpdate;
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
				ChunkReady();
				TextureReady();
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

	// private static Cell2D[,] CreateCells(int length, int width)
	// {
	// 	Cell2D[,] cells = new Cell2D[length, width];
	// 	for (int i = 0; i < length; i++)
	// 	{
	// 		for (int j = 0; j < width; j++)
	// 		{
	// 			cells[i, j] = new Cell2D(RandRange(-100, 100), new Vector3(i, j, 0));
	// 		}
	// 	}
	// 	return cells;
	// }

	// private void Calculate(Cell[,] Cells)
	// {
	// 	int Length = Cells.GetLength(0);
	// 	int Width = Cells.GetLength(1);

	// 	for (int i = 0; i < Length; i++)
	// 	{
	// 		for (int j = 0; j < Width; j++)
	// 		{
	// 			float localT = Cells[i, j].Temperature;
	// 			float deltaT = 0;
	// 			if (i + 1 < Length)
	// 				deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 			else
	// 				deltaT += (Cells[0, j].Temperature - localT) * Conductivity;
	// 			if (i - 1 >= 0)
	// 				deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 			else
	// 				deltaT += (Cells[Length - 1, j].Temperature - localT) * Conductivity;
	// 			if (j + 1 < Width)
	// 				deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 			else
	// 				deltaT += (Cells[i, 0].Temperature - localT) * Conductivity;
	// 			if (j - 1 >= 0)
	// 				deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 			else
	// 				deltaT += (Cells[i, Width - 1].Temperature - localT) * Conductivity;

	// 			Cells[i, j].Temperature += deltaT;
	// 		}
	// 	}
	// }

	private void ChunkReady()
	{
		Chunks.Clear();
		Chunks.Add("Up", new(Toward.Up, CellResolution));
		Chunks.Add("Down", new(Toward.Down, CellResolution));
		Chunks.Add("Left", new(Toward.Left, CellResolution));
		Chunks.Add("Right", new(Toward.Right, CellResolution));
		Chunks.Add("Forward", new(Toward.Forward, CellResolution));
		Chunks.Add("Back", new(Toward.Back, CellResolution));

		Chunks["Up"].LeftNeighbor = Chunks["Left"];
		Chunks["Up"].RightNeighbor = Chunks["Right"];
		Chunks["Up"].UpNeighbor = Chunks["Back"];
		Chunks["Up"].DownNeighbor = Chunks["Forward"];

		Chunks["Down"].LeftNeighbor = Chunks["Left"];
		Chunks["Down"].RightNeighbor = Chunks["Right"];
		Chunks["Down"].UpNeighbor = Chunks["Forward"];
		Chunks["Down"].DownNeighbor = Chunks["Back"];

		Chunks["Left"].LeftNeighbor = Chunks["Back"];
		Chunks["Left"].RightNeighbor = Chunks["Forward"];
		Chunks["Left"].UpNeighbor = Chunks["Up"];
		Chunks["Left"].DownNeighbor = Chunks["Down"];

		Chunks["Right"].LeftNeighbor = Chunks["Forward"];
		Chunks["Right"].RightNeighbor = Chunks["Back"];
		Chunks["Right"].UpNeighbor = Chunks["Up"];
		Chunks["Right"].DownNeighbor = Chunks["Down"];

		Chunks["Forward"].LeftNeighbor = Chunks["Left"];
		Chunks["Forward"].RightNeighbor = Chunks["Right"];
		Chunks["Forward"].UpNeighbor = Chunks["Up"];
		Chunks["Forward"].DownNeighbor = Chunks["Down"];


		Chunks["Back"].LeftNeighbor = Chunks["Right"];
		Chunks["Back"].RightNeighbor = Chunks["Left"];
		Chunks["Back"].UpNeighbor = Chunks["Up"];
		Chunks["Back"].DownNeighbor = Chunks["Down"];
	}

	private void TextureUpdate()
	{
		foreach (var chunk in Chunks.Values)
		{
			Image image = Image.CreateEmpty(CellResolution, CellResolution, false, Image.Format.Rgb8);
			for (int i = 0; i < chunk.Cells.GetLength(0); i++)
			{
				for (int j = 0; j < chunk.Cells.GetLength(1); j++)
				{
					image.SetPixel(i, j, GetHeatColor_S(chunk.Cells[i, j].Temperature));
				}
			}
			chunk.textureRect.Texture = ImageTexture.CreateFromImage(image);
		}
	}

	private void TextureReady()
	{
		foreach (var chunk in Chunks.Values)
		{
			Image image = Image.CreateEmpty(CellResolution, CellResolution, false, Image.Format.Rgb8);
			for (int i = 0; i < chunk.Cells.GetLength(0); i++)
			{
				for (int j = 0; j < chunk.Cells.GetLength(1); j++)
				{
					image.SetPixel(i, j, GetHeatColor_S(chunk.Cells[i, j].Temperature));
				}
			}
			TextureRect rect = TextureRect.Duplicate() as TextureRect;
			rect.Size = new Vector2(CellResolution * CellSize, CellResolution * CellSize);
			rect.Texture = ImageTexture.CreateFromImage(image);
			AddChild(rect);
			chunk.textureRect = rect;

			if (chunk.toward == Toward.Up)
				rect.Position = new(0, -CellResolution * CellSize);
			else if (chunk.toward == Toward.Down)
				rect.Position = new(0, CellResolution * CellSize);
			else if (chunk.toward == Toward.Left)
				rect.Position = new(-CellResolution * CellSize, 0);
			else if (chunk.toward == Toward.Right)
				rect.Position = new(CellResolution * CellSize, 0);
			else if (chunk.toward == Toward.Forward)
				rect.Position = new(0, 0);
			else if (chunk.toward == Toward.Back)
				rect.Position = new(CellResolution * CellSize * 2, 0);
		}
	}

	private void Calculate()
	{
		foreach (Chunk chunk in Chunks.Values)
			chunk.Calculate(Conductivity);

	}

	// private void ChangeColor()
	// {
	// 	int Length = Cells.GetLength(0);
	// 	int Width = Cells.GetLength(1);

	// 	if (Image is null)
	// 	{
	// 		TextureRect.Size = new Vector2(Length * CellSize, Width * CellSize);
	// 		Image = Image.CreateEmpty(Length, Width, false, Image.Format.Rgb8);
	// 	}
	// 	for (int i = 0; i < Length; i++)
	// 	{
	// 		for (int j = 0; j < Width; j++)
	// 		{
	// 			Color color = GetHeatColor_S(Cells[i, j].Temperature);
	// 			Image.SetPixel(i, j, color);
	// 		}
	// 	}
	// 	TextureRect.Texture = ImageTexture.CreateFromImage(Image);
	// }

}



public class Cell2D
{
	public float Temperature;
	public Vector2 GeoCoordinate;
	public Vector3 LocalPosition;

	// public Cell2D(float _temperature, Vector3 _localPosition)
	// {
	// 	Temperature = _temperature;
	// 	LocalPosition = _localPosition;
	// 	SetGeoCoordinate();
	// }

	public Cell2D(Vector3 _localPosition)
	{
		LocalPosition = _localPosition;
		SetGeoCoordinate();
		SetTemperature();
	}

	public void SetGeoCoordinate()
	{
		GeoCoordinate = new(MathF.Atan2(LocalPosition.Y, MathF.Sqrt(MathF.Pow(LocalPosition.X, 2) + MathF.Pow(LocalPosition.Z, 2))),
							MathF.Atan2(LocalPosition.X, -LocalPosition.Z));
	}

	public void SetTemperature()
	{
		// Temperature = RandRange(-100, 100);
		// Temperature = -MathF.Abs(GeoCoordinate.X) * 50 + RandRange(-10, 30);
		// Temperature = GeoCoordinate.X * 57.3f;
		// Temperature = GeoCoordinate.Y * 20f;
		// if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.X)) % 10 <= 5)
		// 	Temperature = 100;
		// else if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.Y)) % 10 <= 5)
		// 	Temperature = -100;
		// else
		// 	Temperature = 0;
	}
}

public class Chunk
{
	public Vector3 toward;
	public Cell2D[,] Cells;
	public int CellResolution;

	public Chunk LeftNeighbor;
	public Chunk RightNeighbor;
	public Chunk UpNeighbor;
	public Chunk DownNeighbor;

	public TextureRect textureRect;

	public Chunk(Vector3 _toward, int _cellResolution)
	{
		toward = _toward;
		CellResolution = _cellResolution;
		Cells = new Cell2D[CellResolution, CellResolution];
		GenerateCells();
	}

	~Chunk()
	{
		Print("clean");
		textureRect.QueueFree();
	}

	public void SetNeighbor(Chunk _LeftNeighbor, Chunk _RightNeighbor, Chunk _UpNeighbor, Chunk _DownNeighbor)
	{
		LeftNeighbor = _LeftNeighbor;
		RightNeighbor = _RightNeighbor;
		UpNeighbor = _UpNeighbor;
		DownNeighbor = _DownNeighbor;
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
				if (toward == Toward.Up)
					innerPos = new Vector3(i - radius, 0, -(j - radius));
				else if (toward == Toward.Down)
					innerPos = new Vector3(i - radius, 0, j - radius);
				else if (toward == Toward.Left)
					innerPos = new Vector3(0, -(j - radius), -(i - radius));
				else if (toward == Toward.Right)
					innerPos = new Vector3(0, -(j - radius), i - radius);
				else if (toward == Toward.Forward)
					innerPos = new Vector3(i - radius, -(j - radius), 0);
				else if (toward == Toward.Back)
					innerPos = new Vector3(-(i - radius), -(j - radius), 0);
				else
				{
					innerPos = Vector3.Zero;
					Print("Chunk/GenerateCells:wtf你为什么没输入一个合理的法向量");
				}
				localPos += innerPos;
				Cells[i, j] = new Cell2D(localPos);
				Cells[i, j].Temperature = i - j;
				// Cells[i, j].Temperature = j-i;
			}
		}
	}

	public void Calculate(float Conductivity)
	{
		if (toward == Toward.Up)
		{

		}
		else if (toward == Toward.Down)
		{

		}
		else if (toward == Toward.Left)
		{

		}
		else if (toward == Toward.Right)
		{

		}
		else if (toward == Toward.Forward)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					float localT = Cells[i, j].Temperature;
					float deltaT = 0;
					if (i + 1 < CellResolution)
						deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
					else
						deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
					if (i - 1 >= 0)
						deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
					else
						deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
					if (j + 1 < CellResolution)
						deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
					else
						deltaT += (UpNeighbor.Cells[i, 0].Temperature - localT) * Conductivity;
					if (j - 1 >= 0)
						deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
					else
						deltaT += (DownNeighbor.Cells[i, CellResolution - 1].Temperature - localT) * Conductivity;

					Cells[i, j].Temperature += deltaT;
				}
			}
		}
		else if (toward == Toward.Back)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					float localT = Cells[i, j].Temperature;
					float deltaT = 0;
					if (i + 1 < CellResolution)
						deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
					else
						deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
					if (i - 1 >= 0)
						deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
					else
						deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
					if (j + 1 < CellResolution)
						deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
					else
						deltaT += (UpNeighbor.Cells[(CellResolution - 1) - i, CellResolution - 1].Temperature - localT) * Conductivity;
					if (j - 1 >= 0)
						deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
					else
						deltaT += (DownNeighbor.Cells[i, CellResolution - 1].Temperature - localT) * Conductivity;

					Cells[i, j].Temperature += deltaT;
				}
			}
		}
		else
		{
			Print("Chunk/Calculate:???你的toward哪去了?");
		}
	}
}

public struct Toward
{
	public static readonly Vector3 Up = Vector3.Up;
	public static readonly Vector3 Down = Vector3.Down;
	public static readonly Vector3 Left = Vector3.Left;
	public static readonly Vector3 Right = Vector3.Right;
	public static readonly Vector3 Forward = Vector3.Forward;
	public static readonly Vector3 Back = Vector3.Back;
}

