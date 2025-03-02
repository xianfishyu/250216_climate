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

	[Export]
	private Vector2 CameraGeoCoord
	{
		get => new(MathF.Atan2(Position.X, -Position.Z),
				MathF.Atan2(Position.Y, MathF.Sqrt(MathF.Pow(Position.X, 2) + MathF.Pow(Position.Z, 2))));
		set
		{
			if (value.Y > 1.552555555555556)
				value.Y = 1.552555555555556f;
			Position = new Vector3(
				MathF.Cos(value.Y) * MathF.Sin(value.X),
				MathF.Sin(value.Y),
				-MathF.Cos(value.Y) * MathF.Cos(value.X)) * CameraScale * 半径;
		}
	}

	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		ScaleUpdate();
		MouseMoveUpdate();

		LookAt(Vector3.Zero);

	}

	private void MouseMoveUpdate()
	{
		Vector2 deltaPos = mousePos - GetViewport().GetMousePosition();
		mousePos = GetViewport().GetMousePosition();

		//你是看不懂这段代码的
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			CameraGeoCoord -= deltaPos / Mathf.Tau / 100 * (CameraScale -0.9f);
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
