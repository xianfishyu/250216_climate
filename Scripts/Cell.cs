using Godot;
using System;
using static Godot.GD;


public partial class Cell : MeshInstance3D
{
	StandardMaterial3D material3D;
	public float Temperature
	{
		get
		{
			temperature ??= 0;
			return (float)temperature;
		}
		set
		{
			temperature = value;
			SetColor(Temperature, material3D);

		}
	}
	private float? temperature;

	public override void _Ready()
	{
		material3D = new StandardMaterial3D();
		MaterialOverride = material3D;
		SetColor(Temperature, material3D);
		// Print(temperature);
	}

	private static void SetColor(float _temperature, StandardMaterial3D material3D)
	{
		Color color = Color.FromHsv(240.0f / 360.0f - _temperature / 255, 1.0f, 1.0f);
		material3D.AlbedoColor = color;
	}
}
