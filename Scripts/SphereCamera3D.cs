using Godot;
using System;
using static Godot.GD;
using static Earth;
using static Tool;

public partial class SphereCamera3D : Camera3D
{
	private Vector2 mousePos = new();

	[Export] private float MinScale = 1.1f;
	[Export] private float MaxScale = 10f;
	[Export]
	private float CameraScale
	{
		get => field;
		set
		{
			if (value > MinScale && value < MaxScale)
				field = value;
			else
				field = Mathf.Clamp(value, MinScale, MaxScale);
		}
	}

	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		ScaleUpdate();
		MouseMoveUpdate();

		Rotation = new Quaternion(Vector3.Forward, -Position.Normalized()).Normalized().GetEuler();
		Rotation = new(Rotation.X, Rotation.Y, 0);

	}

	private void MouseMoveUpdate()
	{
		Vector2 deltaPos = mousePos - GetViewport().GetMousePosition();
		mousePos = GetViewport().GetMousePosition();
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Print(deltaPos);
			Quaternion quaternion = Quaternion.FromEuler(new Vector3(0, deltaPos.X * (CameraScale - 0.8f) / 1000, deltaPos.Y * (CameraScale - 0.8f) / 1000));
			Position = quaternion * Position;
		}
	}

	private void ScaleUpdate()
	{
		Position = Position.Lerp(Position.Normalized() * CameraScale * 半径, 0.1f);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
		{
			switch (mouseEvent.ButtonIndex)
			{
				//放大
				case MouseButton.WheelUp:
					CameraScale /= 1.1f;
					break;

				//缩小
				case MouseButton.WheelDown:
					CameraScale *= 1.1f;
					break;
				default:
					break;
			}
		}
	}
}
