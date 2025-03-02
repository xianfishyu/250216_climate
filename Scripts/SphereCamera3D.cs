using Godot;
using System;
using static Godot.GD;
using static Earth;
using static Tool;

public partial class SphereCamera3D : Camera3D
{
	private Vector2 mousePos = new();

	private float Zoom
	{
		get => Position.Length();
		set;
	}

	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		Rotation = new Quaternion(Vector3.Forward, -Position.Normalized()).GetEuler();
		Rotation = new(Rotation.X, Rotation.Y, 0);
		// MouseMoveUpdate();
	}

	private void MouseMoveUpdate()
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Vector2 deltaPos = mousePos - GetViewport().GetMousePosition();
			Print(deltaPos);

			// Position += deltaPos;
			// nextPos = Position;
		}
	}


	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
		{
			switch (mouseEvent.ButtonIndex)
			{
				//放大
				case MouseButton.WheelUp:
					if (Position.Length() > 1.1 * 半径)
						Position /= 1.1f;
					break;

				//缩小
				case MouseButton.WheelDown:
					if (Position.Length() < 10 * 半径)
						Position *= 1.1f;
					Print(1);
					break;
				default:
					break;
			}
		}
	}
}
