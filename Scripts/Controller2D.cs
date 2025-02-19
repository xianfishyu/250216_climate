using Godot;
using System;
using static Godot.GD;

[Tool]
public partial class Controller2D : Node2D
{
	// [Export] float temperature = 0;
	float[] temp = new float[200];

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
		for (int i = 0; i < temp.Length; i++)
		{
			temp[i] = i - 100;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// ChangeTemperature();
		// QueueRedraw();
	}

	public override void _Draw()
	{
		// DrawCircle(new Vector2(0, 0), 10, GetHeatColor(temperature));
		for (int i = 0; i < temp.Length; i++)
		{
			DrawCircle(new Vector2(i * 10, 0), 5, GetHeatColor(temp[i]));
		}
	}

	// private void ChangeTemperature()
	// {
	// 	if (temperature > 100)
	// 		temperature = -100;
	// 	else
	// 		temperature += 0.1f;
	// }

	private Color GetHeatColor(float temperature)
	{
		if (temperature < ColdThreshold)
			return ColdColor;
		else if (temperature < ZeroThreshold)
		{
			float t = MathF.Abs(temperature - ColdThreshold) / MathF.Abs(ZeroThreshold - ColdThreshold);
			Color color = ColdColor;
			color.S = 1 - t;
			return color;
		}
		else if (temperature == ZeroThreshold)
			return Colors.White;
		else if (temperature < HotThreshold)
		{
			float t = MathF.Abs(temperature - HotThreshold) / MathF.Abs(ZeroThreshold - HotThreshold);
			Color color = HotColor;
			color.S = 1 - t;
			return color;
		}
		else
			return HotColor;
	}
}
