using System;
using Godot;
using static Godot.GD;
using _Climate.Scripts;

public partial class Controller : Node
{
    [Export] PackedScene cellScene;
    MeshInstance3D cellPrefab;

    [Export] float cellSize = 0.1f;
    [Export] int Width = 100;
    [Export] int Height = 100;
    [Export] Double Alpha = 1e-4;
    Cell[,] cells;
    
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
        _temperatureCalculator = new TemperatureCalculator(Width, Height, Alpha);
        cells = new Cell[Width, Height];
        cellPrefab = cellScene.Instantiate<MeshInstance3D>();
        
        // Create cells
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                MeshInstance3D cell = cellPrefab.Duplicate() as MeshInstance3D;
                AddChild(cell);
                cell.Scale = new Vector3(cellSize, cellSize, cellSize);
                cell.Position = new Vector3(x * cellSize, 0, y * cellSize);
                cells[x, y] = cell as Cell;
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
            var width = RandRange(radius, _temperatureCalculator.Width - radius);
            var height = RandRange(radius, _temperatureCalculator.Height - radius);
            var temperature = RandRange(-54, 56);
            
            for (var x = 0; x < _temperatureCalculator.Width; x++)
            {
                for (var y = 0; y < _temperatureCalculator.Height; y++)
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
                for (int x = 0; x < _temperatureCalculator.Width; x++)
                {
                    for (int y = 0; y < _temperatureCalculator.Height; y++)
                    {
                        cells[x, y].Temperature = (float)_temperatureCalculator.Cells[x, y];
                    }
                }
                break;
            case MapType.TemperatureChangeDistribution:
                for (int x = 0; x < _temperatureCalculator.Width; x++)
                {
                    for (int y = 0; y < _temperatureCalculator.Height; y++)
                    {
                        cells[x, y].Temperature = (float)_temperatureCalculator.CellsDerivative[x, y];
                    }
                }
                break;
            case MapType.TemperatureAnomalyDistribution:
                for (int x = 0; x < _temperatureCalculator.Width; x++)
                {
                    for (int y = 0; y < _temperatureCalculator.Height; y++)
                    {
                        cells[x, y].Temperature = (float)_temperatureCalculator.CellsAnomaly[x, y];
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


