using Godot;
using _Climate.Scripts;

public partial class Controller : Node
{
    [Export] PackedScene cellScene;
    MeshInstance3D cellPrefab;

    [Export] float cellSize = 0.1f;
    [Export] int Width = 100;
    [Export] int Height = 100;
    Cell[,] cells;


    CellCalculator cellCalculator;
    
    public override void _Ready()
    {
        cellCalculator = new CellCalculator(Width, Height);
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
        cellCalculator.Calculate(delta);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                cells[x, y].Temperature = cellCalculator.cells[x, y].Temperature;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.R)
            {
                cellCalculator.ClearCells();
            }
        }
    }
}


