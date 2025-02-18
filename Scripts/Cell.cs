using Godot;
using System;
using static Godot.GD;

public partial class Cell : MeshInstance3D
{
	public float Temperature
	{
		get
		{
			temperature ??= RandRange(-10, 50);
			return (float)temperature;
		}
		set
		{
			temperature = value;
			SetShaderColor(shaderMaterial, Temperature);
		}
	}
	private float? temperature;

	[Export] Shader shader;
	ShaderMaterial shaderMaterial = new();

	public override void _Ready()
	{

		shaderMaterial.Shader = shader;
		MaterialOverride = shaderMaterial;

		SetShaderColor(shaderMaterial, Temperature);
	}


	private static void SetShaderColor(ShaderMaterial shaderMaterial, float _temperature, string name = "color")
	{
		Vector3 color = new(1, _temperature / 255, 0);
		shaderMaterial.SetShaderParameter(name, color);
	}

}
