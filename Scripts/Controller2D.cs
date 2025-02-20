using Godot;
using Godot.Collections;
using System;
using System.Threading;
using static Godot.GD;

// [Tool]
public partial class Controller2D : Node2D
{
	private Cell2D[,] Cells;
	private float Time = 0;
	private float DeltaTime = 0.01f;

	[ExportCategory("CellsSettings")]
	[Export] private int Length = 200;
	[Export] private int Width = 100;
	[Export] private float CellSize = 10;
	[Export] private float Conductivity = 0.1f;

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
		Cells = CreateCells(Length, Width, CellSize);
		Timer.Timeout += Calculate;
		Timer.Timeout += TimeUpdate;
	}

	public override void _PhysicsProcess(double delta)
	{
		QueueRedraw();
	}



	public override void _Draw()
	{
		foreach (var cell in Cells)
		{
			Color color = GetHeatColor_S(cell.Temperature);
			DrawRect(new Rect2(cell.LocalPosition, new Vector2(CellSize, CellSize)), color);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.R)
			{
				Cells = CreateCells(Length, Width, CellSize);
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

	private static Cell2D[,] CreateCells(int length, int width, float size)
	{
		Cell2D[,] cells = new Cell2D[length, width];
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < width; j++)
			{
				cells[i, j] = new Cell2D(RandRange(-100, 100), new Vector2(i * size, j * size));
			}
		}
		return cells;
	}

	private void Calculate()
	{

		for (int i = 0; i < Length; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				float localT = Cells[i, j].Temperature;
				float deltaT = 0;
				if (i + 1 < Length)
					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
				if (i - 1 >= 0)
					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
				if (j + 1 < Width)
					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
				if (j - 1 >= 0)
					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;

				Cells[i, j].Temperature += deltaT;
			}
		}
	}

	private void TimeUpdate()
	{
		Time = (Time + DeltaTime) % 1;
	}

}


public class Cell2D
{
	public float Temperature;
	// public Vector2 GeoCoordinate;
	public Vector2 LocalPosition;

	public Cell2D(float _temperature, Vector2 _position)
	{
		Temperature = _temperature;
		LocalPosition = _position;
	}
}