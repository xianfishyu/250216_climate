using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Earth;
using static Tool;


[Tool]
public partial class SphereGroup : Node3D
{
	[Export] private Dictionary<StringName, NodePath> Planes = [];
	[ExportToolButton("Reset")] private Callable Reset => Callable.From(PlanesUpdate);

	public override void _Ready()
	{
		PlanesUpdate();
	}

	private void PlanesUpdate()
	{
		foreach (var plane in Planes.Values)
		{
			if (plane is null)
				Print("SphereGruop/PlanesUpdate:Planes是空值喵");

			MeshInstance3D Node = GetNode<MeshInstance3D>(plane);
			Node.Mesh = new PlaneMesh
			{
				Size = new Vector2(半径 * 2, 半径 * 2),
				SubdivideWidth = Subsurf,
				SubdivideDepth = Subsurf
			};
			Node.Position = Node.Position.Normalized() * 半径;
		}
	}

	public override void _Process(double delta)
	{

	}
}
