using System;
using Godot;
using static Godot.GD;
using _Climate.Scripts;

public partial class Controller : Node
{
	[Export] PackedScene cellScene;
	MeshInstance3D cellPrefab;

	[Export] float CellSize = 0.1f;
	[Export] int Length = 50;
	// [Export] int Width = 20;
	// [Export] int Height = 20;
	[Export] double Alpha = 1e-4;
	//Cell[,,] cells_left, cells_front, cells_right, cells_back, cells_top, cells_button;
	Cell[,,] cells;

	// 温度分布图，变温分布图，气温距平分布图
	private enum MapType
	{
		TemperatureDistribution,
		TemperatureChangeDistribution,
		TemperatureAnomalyDistribution,
		TemperatureNum
	}

	private MapType _mapType = MapType.TemperatureDistribution;


	TemperatureCalculator _temperatureCalculator;

	public override void _Ready()
	{
		_temperatureCalculator = new TemperatureCalculator(Length, Alpha);
		
		cells = new Cell[Length, Length, Length];
		cellPrefab = cellScene.Instantiate<MeshInstance3D>();
		
		// Create cells

		 for (int i = 0; i < Length; i++)
		{
			for (int j = 0; j < Length; j++)  // Width
			{
				for (int k = 0; k < Length; k++)  // Height
				{
					if ((i == 0 || i == Length - 1) || (j == 0 || j == Length - 1) || (k == 0 || k == Length - 1))
					{
						MeshInstance3D cell = cellPrefab.Duplicate() as MeshInstance3D;
						AddChild(cell);
						cell.Scale = new Vector3(CellSize, CellSize, CellSize);
						cell.Position = new Vector3(
							(i - Length / 2) * CellSize,
							(j - Length / 2) * CellSize,
							(k - Length / 2) * CellSize
						);
						cells[i, j, k] = cell as Cell;
					}
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		_temperatureCalculator.Calculate(delta);

		// 随机生成温度
		if (Randf() < 0.1)
		{
			// cellCalculator.cells[RandRange(0, cellCalculator.width - 1), RandRange(0, cellCalculator.height - 1)]
			//     .Temperature = RandRange(-10, 255);
			var radius = RandRange(1, 10);
			var width = RandRange(radius, _temperatureCalculator.Length - radius);
			var height = RandRange(radius, _temperatureCalculator.Length - radius);
			var temperature = RandRange(-100, 100);

			for (var x = 0; x < _temperatureCalculator.Length; x++)
			{
				for (var y = 0; y < _temperatureCalculator.Length; y++)
				{
					if (Mathf.Pow(x - width, 2) + Mathf.Pow(y - height, 2) < radius)
					{
						_temperatureCalculator.Cells[x, y] = temperature;
					}
				}
			}
		}

		switch (_mapType)
		{
			case MapType.TemperatureDistribution:
				for (int x = 0; x < _temperatureCalculator.Length; x++)
				{
					for (int y = 0; y < _temperatureCalculator.Length; y++)
					{
						//cells_left[0, x, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						//cells_button[x, 0, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						//cells_back[x, y, 0].Temperature = (float)_temperatureCalculator.Cells[x, y];
						//cells_right[Length - 1, x, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						//cells_top[x, Width - 1, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						//cells_front[x, y, Height - 1].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[0, x, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[x, 0, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[x, y, 0].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[Length - 1, x, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[x, Length - 1, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
						cells[x, y, Length - 1].Temperature = (float)_temperatureCalculator.Cells[x, y];
					}
				}

				break;
			case MapType.TemperatureChangeDistribution:
				for (int x = 0; x < _temperatureCalculator.Length; x++)
				{
					for (int y = 0; y < _temperatureCalculator.Length; y++)
					{
						cells[x, 0, y].Temperature = (float)_temperatureCalculator.CellsDerivative[x, y];
					}
				}

				break;
			case MapType.TemperatureAnomalyDistribution:
				for (int x = 0; x < _temperatureCalculator.Length; x++)
				{
					for (int y = 0; y < _temperatureCalculator.Length; y++)
					{
						cells[x, 0, y].Temperature = (float)_temperatureCalculator.CellsAnomaly[x, y];
					}
				}

				break;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.R)
			{
				_temperatureCalculator.ClearCells();
			}

			if (keyEvent.Keycode == Key.T)
			{
				Print(_mapType);
				if (_mapType < MapType.TemperatureNum - 1)
				{
					_mapType++;
				}
				else
				{
					_mapType = 0;
				}
			}
		}
	}
}
