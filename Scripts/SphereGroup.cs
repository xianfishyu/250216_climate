using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Earth;
using static Tool;


[Tool]
public partial class SphereGroup : Node3D
{
	[Export] private int Subsurf = 100;
	[ExportToolButton("Reset")] private Callable Reset => Callable.From(PlanesReady);

	[Export] private Dictionary<StringName, NodePath> Planes = [];

	[Export] private Dictionary<string,Image> PlanesTexture = [];

	public override void _Ready()
	{
		PlanesReady();
	}

	private void PlanesReady()
	{
		foreach (var plane in Planes.Values)
		{
			if (plane is null)
				Print("SphereGruop/PlanesUpdate:Planes是空值喵");

			MeshInstance3D Node = GetNode<MeshInstance3D>(plane);

			Vector3 offset;
			Vector2 Size;
			bool Flip = false;

			PlaneMesh newMesh = Node.Mesh as PlaneMesh;
			newMesh.SubdivideWidth = Subsurf;
			newMesh.SubdivideDepth = Subsurf;

			if (Planes["Up"] == plane)
			{
				offset = Vector3.Up;
				Size = new Vector2(半径 * 2, 半径 * 2);
			}
			else if (Planes["Down"] == plane)
			{
				offset = Vector3.Down;
				Size = new Vector2(半径 * 2, 半径 * 2);
				Flip = true;

			}
			else if (Planes["Left"] == plane)
			{
				offset = Vector3.Back;
				Size = new Vector2(半径 * 2, 半径 * 2);

			}
			else if (Planes["Right"] == plane)
			{
				offset = Vector3.Forward;
				Size = new Vector2(半径 * 2, 半径 * 2);
				Flip = true;
			}
			else if (Planes["Front"] == plane)
			{
				offset = Vector3.Right;
				Size = new Vector2(半径 * 2, 半径 * 2);
			}
			else if (Planes["Back"] == plane)
			{
				offset = Vector3.Left;
				Size = new Vector2(半径 * 2, 半径 * 2);
				Flip = true;
			}
			else
			{
				offset = Vector3.Zero;
				Size = Vector2.Zero;
			}

			newMesh.CenterOffset = offset * 半径;
			newMesh.Size = Size;
			newMesh.FlipFaces = Flip;

		}
	
		PlanesTexture.Add("Up",new Image());
		PlanesTexture.Add("Down",new Image());
		PlanesTexture.Add("Left",new Image());
		PlanesTexture.Add("Right",new Image());
		PlanesTexture.Add("Front",new Image());
		PlanesTexture.Add("Back",new Image());
		
	}

	public override void _Process(double delta)
	{

	}
}
