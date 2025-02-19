using Godot;
using System;
using System.ComponentModel;
using static Godot.GD;

public partial class Controller : Node
{
	[Export] PackedScene cellScene;
	MeshInstance3D cellPrefab;

	[Export] float cellSize = 0.1f;
	[Export] int cellsCount = 100;
	Cell[,] cells;

	public override void _Ready()
	{
		cellPrefab = cellScene.Instantiate<MeshInstance3D>();

		cells = new Cell[cellsCount, cellsCount];
		CreateCells();
	}

	public override void _Process(double delta)
	{
		Cell[,] CellsUpdate = cells;
		for (int x = 0; x < cellsCount; x++)
		{
			for (int y = 0; y < cellsCount; y++)
			{
				if (x + 1 < cellsCount)
					CellsUpdate[x, y].Temperature += (cells[x + 1, y].Temperature - cells[x, y].Temperature) / 1000;
				if (y + 1 < cellsCount)
					CellsUpdate[x, y].Temperature += (cells[x, y + 1].Temperature - cells[x, y].Temperature) / 1000;
				if (x - 1 > -1)
					CellsUpdate[x, y].Temperature += (cells[x - 1, y].Temperature - cells[x, y].Temperature) / 1000;
				if (y - 1 > -1)
					CellsUpdate[x, y].Temperature += (cells[x, y - 1].Temperature - cells[x, y].Temperature) / 1000;
			}
		}
		cells = CellsUpdate;
		if (Randf() < 0.01)
			cells[RandRange(0, cellsCount - 1), RandRange(0, cellsCount - 1)].Temperature = RandRange(-10, 500);
	}

	private void CreateCells()
	{
		for (int x = 0; x < cellsCount; x++)
		{
			for (int y = 0; y < cellsCount; y++)
			{
				MeshInstance3D cell = cellPrefab.Duplicate() as MeshInstance3D;
				AddChild(cell);
				cell.Scale = new Vector3(cellSize, cellSize, cellSize);
				cell.Position = new Vector3(x * cellSize, 0, y * cellSize);
				cells[x, y] = cell as Cell;
			}
		}
	}
}
