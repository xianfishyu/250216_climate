using Godot;
using System;
using static Godot.GD;

[Tool]
public partial class Controller2D : Node2D
{

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
		
	}

	public override void _PhysicsProcess(double delta)
	{
		// QueueRedraw();
	}

	public override void _Draw()
	{
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
			Print(t);
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
}
