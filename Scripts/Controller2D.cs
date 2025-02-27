using Godot;
using System;
using System.Collections.Generic;
using static Godot.GD;

// [Tool]
public partial class Controller2D : Node2D
{
	private Dictionary<string, Chunk> Chunks = new();
	private List<Cell2D> CellList = new();

	[ExportCategory("CellsSettings")]
	[Export] private int CellResolution = 100;
	[Export] private float Conductivity = 0.1f;

	[ExportCategory("TextureSettings")]
	[Export] private PackedScene ChunkPrefab;
	private TextureRect TextureRect;
	[Export] private float CellSize = 1f;
	private Image Image;

	[Export] private Godot.Timer Timer;

	[ExportCategory("HeatSettings")]
	[ExportGroup("ColorSettings")]
	[Export] private Color ColdColor = new(0, 0, 1);
	[Export] private Color HotColor = new(1, 0, 0);
	[ExportGroup("TempSettings")]
	[Export] private float ColdThreshold = -100;
	[Export] private float ZeroThreshold = 0;
	[Export] private float HotThreshold = 100;

	public override void _Ready()
	{
		TextureRect = ChunkPrefab.Instantiate<TextureRect>();

		ChunkReady();
		TextureReady();

		Timer.Timeout += Calculate;
		Timer.Timeout += TextureUpdate;
	}

	public override void _PhysicsProcess(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.R)
			{
				ChunkReady();
				TextureReady();
			}
		}
	}

	private Color GetHeatColor_H_OutOfRange(float temperature)
	{
		float range = Mathf.Abs(HotThreshold - ColdThreshold);
		float t = Mathf.Abs(temperature - ColdThreshold) / range * (240f / 359f);
		Color color = new(1, 0, 0);
		color.H = t;
		return color;
	}

	private Color GetHeatColor_H(float temperature)
	{
		if (temperature < ColdThreshold)
			temperature = ColdThreshold;
		else if (temperature > HotThreshold)
			temperature = HotThreshold;
		float range = Mathf.Abs(HotThreshold - ColdThreshold);
		float t = Mathf.Abs(temperature - ColdThreshold) / range * (240f / 359f);
		Color color = new(1, 0, 0);
		color.H = t;
		return color;
	}

	private Color GetHeatColor_S(float temperature)
	{
		if (temperature < ColdThreshold)
			return ColdColor;
		else if (temperature < ZeroThreshold)
		{
			float t = Mathf.Abs(temperature - ColdThreshold) / Mathf.Abs(ZeroThreshold - ColdThreshold);
			Color color = ColdColor;
			color.S = 1 - t;
			return color;
		}
		else if (temperature == ZeroThreshold)
			return Colors.White;
		else if (temperature < HotThreshold)
		{
			float t = Mathf.Abs(temperature - HotThreshold) / Mathf.Abs(ZeroThreshold - HotThreshold);
			Color color = HotColor;
			color.S = 1 - t;
			return color;
		}
		else
			return HotColor;
	}

	private void ChunkReady()
	{
		foreach (Chunk chunk in Chunks.Values)
			chunk.textureRect?.QueueFree();
		Chunks.Clear();
		Chunks.Add("Up", new(Toward.Up, CellResolution));
		Chunks.Add("Down", new(Toward.Down, CellResolution));
		Chunks.Add("Left", new(Toward.Left, CellResolution));
		Chunks.Add("Right", new(Toward.Right, CellResolution));
		Chunks.Add("Forward", new(Toward.Forward, CellResolution));
		Chunks.Add("Back", new(Toward.Back, CellResolution));

		Chunks["Up"].LeftNeighbor = Chunks["Left"];
		Chunks["Up"].RightNeighbor = Chunks["Right"];
		Chunks["Up"].UpNeighbor = Chunks["Back"];
		Chunks["Up"].DownNeighbor = Chunks["Forward"];

		Chunks["Down"].LeftNeighbor = Chunks["Left"];
		Chunks["Down"].RightNeighbor = Chunks["Right"];
		Chunks["Down"].UpNeighbor = Chunks["Forward"];
		Chunks["Down"].DownNeighbor = Chunks["Back"];

		Chunks["Left"].LeftNeighbor = Chunks["Back"];
		Chunks["Left"].RightNeighbor = Chunks["Forward"];
		Chunks["Left"].UpNeighbor = Chunks["Up"];
		Chunks["Left"].DownNeighbor = Chunks["Down"];

		Chunks["Right"].LeftNeighbor = Chunks["Forward"];
		Chunks["Right"].RightNeighbor = Chunks["Back"];
		Chunks["Right"].UpNeighbor = Chunks["Up"];
		Chunks["Right"].DownNeighbor = Chunks["Down"];

		Chunks["Forward"].LeftNeighbor = Chunks["Left"];
		Chunks["Forward"].RightNeighbor = Chunks["Right"];
		Chunks["Forward"].UpNeighbor = Chunks["Up"];
		Chunks["Forward"].DownNeighbor = Chunks["Down"];


		Chunks["Back"].LeftNeighbor = Chunks["Right"];
		Chunks["Back"].RightNeighbor = Chunks["Left"];
		Chunks["Back"].UpNeighbor = Chunks["Up"];
		Chunks["Back"].DownNeighbor = Chunks["Down"];

		foreach (Chunk chunk in Chunks.Values)
			chunk.SetNodeList();

		SetCellList();
	}

	private void TextureReady()
	{
		foreach (Chunk chunk in Chunks.Values)
		{
			Image image = Image.CreateEmpty(CellResolution, CellResolution, false, Image.Format.Rgb8);
			for (int i = 0; i < chunk.Cells.GetLength(0); i++)
			{
				for (int j = 0; j < chunk.Cells.GetLength(1); j++)
				{
					image.SetPixel(i, j, GetHeatColor_H(chunk.Cells[i, j].Temperature));
				}
			}

			TextureRect rect = TextureRect.Duplicate() as TextureRect;
			rect.Size = new Vector2(CellResolution * CellSize, CellResolution * CellSize);
			rect.Texture = ImageTexture.CreateFromImage(image);
			AddChild(rect);
			chunk.textureRect = rect;

			if (chunk.toward == Toward.Up)
				rect.Position = new(0, -CellResolution * CellSize);
			else if (chunk.toward == Toward.Down)
				rect.Position = new(0, CellResolution * CellSize);
			else if (chunk.toward == Toward.Left)
				rect.Position = new(-CellResolution * CellSize, 0);
			else if (chunk.toward == Toward.Right)
				rect.Position = new(CellResolution * CellSize, 0);
			else if (chunk.toward == Toward.Forward)
				rect.Position = new(0, 0);
			else if (chunk.toward == Toward.Back)
				rect.Position = new(CellResolution * CellSize * 2, 0);
		}
	}

	private void TextureUpdate()
	{
		foreach (Chunk chunk in Chunks.Values)
		{
			Image image = Image.CreateEmpty(CellResolution, CellResolution, false, Image.Format.Rgb8);
			for (int i = 0; i < chunk.Cells.GetLength(0); i++)
			{
				for (int j = 0; j < chunk.Cells.GetLength(1); j++)
				{
					image.SetPixel(i, j, GetHeatColor_H(chunk.Cells[i, j].Temperature));
				}
			}
			chunk.textureRect.Texture = ImageTexture.CreateFromImage(image);
		}
	}

	private void SetCellList()
	{
		foreach (Chunk chunk in Chunks.Values)
			foreach (Cell2D cell in chunk.Cells)
				CellList.Add(cell);

	}

	private void Calculate()
	{
		foreach (Cell2D cell in CellList)
		{
			float leftT = cell.left.Temperature;
			float rightT = cell.right.Temperature;
			float upT = cell.up.Temperature;
			float downT = cell.down.Temperature;
			float localT = cell.Temperature;
			float deltaT = 0;

			deltaT += (leftT - localT) * Conductivity;
			deltaT += (rightT - localT) * Conductivity;
			deltaT += (upT - localT) * Conductivity;
			deltaT += (downT - localT) * Conductivity;

			cell.Temperature += deltaT;

			if (Randf() < 0.000001)
			{
				if (Randf() <= 0.5)
					cell.Temperature = -1000;
				else
					cell.Temperature = 1000;
			}

		}
	}

	// private void CalculateNoooooooooooo()
	// {
	// 	foreach (Chunk chunk in Chunks.Values)
	// 		chunk.Calculate(Conductivity);
	// }



	// public void CalculateNew(double delta)
	// {
	// 	int Width = CellResolution;
	// 	int Height = CellResolution;


	// 	double dx2 = 1.0 / ((Width - 1) * (Height - 1));

	// 	// 辅助函数，用于计算温度分布的导数
	// 	double[,] ComputeHeatEquation(double[,] cells, int width, int height, double dx2, double alpha)
	// 	{
	// 		double[,] dTdt = new double[width, height];
	// 		for (int x = 0; x < width; x++)
	// 		{
	// 			for (int y = 0; y < height; y++)
	// 			{
	// 				double d2Tdx2 = 0;
	// 				double d2Tdy2 = 0;

	// 				if (x > 0) d2Tdx2 += cells[x - 1, y];
	// 				if (x < width - 1) d2Tdx2 += cells[x + 1, y];
	// 				if (y > 0) d2Tdy2 += cells[x, y - 1];
	// 				if (y < height - 1) d2Tdy2 += cells[x, y + 1];


	// 				if (x == 0) d2Tdx2 += cells[width - 1, y];
	// 				if (x == width - 1) d2Tdx2 += cells[0, y];
	// 				if (y == 0) d2Tdy2 += cells[x, height - 1];
	// 				if (y == height - 1) d2Tdy2 += cells[x, 0];

	// 				d2Tdx2 -= 2 * cells[x, y];
	// 				d2Tdy2 -= 2 * cells[x, y];

	// 				dTdt[x, y] = alpha * (d2Tdx2 / (dx2) + d2Tdy2 / (dx2)); // 将矩阵展平成向量
	// 			}
	// 		}

	// 		return dTdt;
	// 	}

	// 	// https://i.imgur.com/RIlJM32.png
	// 	// https://zhuanlan.zhihu.com/p/8616433050
	// 	double[,] rk4(double[,] cells, double dt, int width, int height, double dx2, double alpha)
	// 	{
	// 		double[,] T = new double[width, height];

	// 		// 处理内部
	// 		for (int x = 0; x < width; x++)
	// 		{
	// 			for (int y = 0; y < height; y++)
	// 			{
	// 				T[x, y] = cells[x, y];
	// 			}
	// 		}

	// 		// 时间积分：使用 Runge-Kutta 方法
	// 		// 计算k1234
	// 		double[,] k1 = ComputeHeatEquation(cells, width, height, dx2, alpha);
	// 		double[,] k2 = ComputeHeatEquation(cells, width, height, dx2, alpha);
	// 		double[,] k3 = ComputeHeatEquation(cells, width, height, dx2, alpha);
	// 		double[,] k4 = ComputeHeatEquation(cells, width, height, dx2, alpha);

	// 		// 更新u_i^(n+1)
	// 		for (int x = 0; x < width; x++)
	// 		{
	// 			for (int y = 0; y < height; y++)
	// 			{
	// 				T[x, y] += dt / 6 * (k1[x, y] + 2 * k2[x, y] + 2 * k3[x, y] + k4[x, y]);
	// 			}
	// 		}

	// 		return T;
	// 	}

	// 	// 数学逼提醒了我用龙格库塔法求偏微分，让我们赞美数学逼
	// 	List<Cell2D> cellsUpdate = CellList;
	// 	double[,] tNew = rk4(CellList, delta, Width, Height, dx2, Alpha);
	// 	CellsDerivative = ComputeHeatEquation(CellList, Width, Height, dx2, Alpha);

	// 	for (int x = 0; x < Width; x++)
	// 	{
	// 		for (int y = 0; y < Height; y++)
	// 		{
	// 			cellsUpdate[x, y] = (float)tNew[x, y];
	// 		}
	// 	}

	// 	// 距平值计算
	// 	for (int x = 0; x < Width; x++)
	// 	{
	// 		for (int y = 0; y < Height; y++)
	// 		{
	// 			_cellsAverage[x, y] += (CellList[x, y] - _cellsAverage[x, y]) / (_averageCount + 1);
	// 		}
	// 	}

	// 	// 单元格的距平值
	// 	for (int x = 0; x < Width; x++)
	// 	{
	// 		for (int y = 0; y < Height; y++)
	// 		{
	// 			CellsAnomaly[x, y] = CellList[x, y] - _cellsAverage[x, y];
	// 		}
	// 	}

	// 	if (_averageCount < 1000)
	// 		_averageCount++;

	// 	CellList = cellsUpdate;
	// }

}



public class Cell2D
{
	public float Temperature;
	public Vector2 GeoCoordinate;
	public Vector3 LocalPosition;

	public Cell2D up;
	public Cell2D down;
	public Cell2D left;
	public Cell2D right;

	public Cell2D(Vector3 _localPosition)
	{

		LocalPosition = _localPosition;
		SetGeoCoordinate();
		SetTemperature();
	}

	public void SetGeoCoordinate()
	{
		GeoCoordinate = new(MathF.Atan2(LocalPosition.Y, MathF.Sqrt(MathF.Pow(LocalPosition.X, 2) + MathF.Pow(LocalPosition.Z, 2))),
							MathF.Atan2(LocalPosition.X, -LocalPosition.Z));
	}

	public void SetTemperature()
	{
		Temperature = 0;
		// Temperature = RandRange(-100, 100);
		// // Temperature = -MathF.Abs(GeoCoordinate.X) * 50 + RandRange(-10, 30);
		// // Temperature = GeoCoordinate.X * 57.3f;
		// // Temperature = GeoCoordinate.Y * 20f;
		// // if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.X)) % 10 <= 5)
		// // 	Temperature = 100;
		// // else if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.Y)) % 10 <= 5)
		// // 	Temperature = -100;
		// // else
		// // 	Temperature = 0;
	}
}

public class Chunk
{
	public Vector3 toward;
	public Cell2D[,] Cells;
	public int CellResolution;

	public Chunk LeftNeighbor;
	public Chunk RightNeighbor;
	public Chunk UpNeighbor;
	public Chunk DownNeighbor;

	public TextureRect textureRect;

	public Chunk(Vector3 _toward, int _cellResolution)
	{
		toward = _toward;
		CellResolution = _cellResolution;
		Cells = new Cell2D[CellResolution, CellResolution];
		GenerateCells();
	}

	public void SetNeighbor(Chunk _LeftNeighbor, Chunk _RightNeighbor, Chunk _UpNeighbor, Chunk _DownNeighbor)
	{
		LeftNeighbor = _LeftNeighbor;
		RightNeighbor = _RightNeighbor;
		UpNeighbor = _UpNeighbor;
		DownNeighbor = _DownNeighbor;
	}

	public void GenerateCells()
	{
		int radius = CellResolution / 2;
		for (int i = 0; i < CellResolution; i++)
		{
			for (int j = 0; j < CellResolution; j++)
			{
				Vector3 innerPos;
				Vector3 localPos = toward * radius;
				if (toward == Toward.Up)
					innerPos = new Vector3(i - radius, 0, -(j - radius));
				else if (toward == Toward.Down)
					innerPos = new Vector3(i - radius, 0, j - radius);
				else if (toward == Toward.Left)
					innerPos = new Vector3(0, -(j - radius), -(i - radius));
				else if (toward == Toward.Right)
					innerPos = new Vector3(0, -(j - radius), i - radius);
				else if (toward == Toward.Forward)
					innerPos = new Vector3(i - radius, -(j - radius), 0);
				else if (toward == Toward.Back)
					innerPos = new Vector3(-(i - radius), -(j - radius), 0);
				else
				{
					innerPos = Vector3.Zero;
					Print("Chunk/GenerateCells:wtf你为什么没输入一个合理的法向量");
				}
				localPos += innerPos;
				Cells[i, j] = new Cell2D(localPos);
				// if (toward == Toward.Up)
				// 	Cells[i, j].Temperature = 100;
			}

		}
	}

	public void SetNodeList()
	{
		if (toward == Toward.Up)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[(CellResolution - 1) - j, 0];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[j, 0];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[i, 0];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[(CellResolution - 1) - i, 0];
				}
			}
		}
		else if (toward == Toward.Down)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[j, CellResolution - 1];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[(CellResolution - 1) - j, CellResolution - 1];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[(CellResolution - 1) - i, CellResolution - 1];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[i, CellResolution - 1];
				}
			}

		}
		else if (toward == Toward.Left)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[0, j];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[CellResolution - 1, j];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[0, (CellResolution - 1) - i];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[0, i];
				}
			}
		}
		else if (toward == Toward.Right)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[0, j];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[CellResolution - 1, j];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[CellResolution - 1, i];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[CellResolution - 1, (CellResolution - 1) - i];
				}
			}
		}
		else if (toward == Toward.Forward)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[0, j];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[CellResolution - 1, j];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[i, 0];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[i, CellResolution - 1];
				}
			}
		}
		else if (toward == Toward.Back)
		{
			for (int i = 0; i < CellResolution; i++)
			{
				for (int j = 0; j < CellResolution; j++)
				{
					if (i + 1 < CellResolution)
						Cells[i, j].right = Cells[i + 1, j];
					else
						Cells[i, j].right = RightNeighbor.Cells[0, j];
					if (i - 1 >= 0)
						Cells[i, j].left = Cells[i - 1, j];
					else
						Cells[i, j].left = LeftNeighbor.Cells[CellResolution - 1, j];
					if (j + 1 < CellResolution)
						Cells[i, j].down = Cells[i, j + 1];
					else
						Cells[i, j].down = DownNeighbor.Cells[(CellResolution - 1) - i, CellResolution - 1];
					if (j - 1 >= 0)
						Cells[i, j].up = Cells[i, j - 1];
					else
						Cells[i, j].up = UpNeighbor.Cells[(CellResolution - 1) - i, 0];
				}
			}
		}
		else
			Print("Chunk/Calculate:???你的toward哪去了?");

	}

	// public void Calculate(float Conductivity)
	// {
	// 	foreach (Cell2D cell in Cells)
	// 	{
	// 		float leftT = cell.left.Temperature;
	// 		float rightT = cell.right.Temperature;
	// 		float upT = cell.up.Temperature;
	// 		float downT = cell.down.Temperature;
	// 		float localT = cell.Temperature;
	// 		float deltaT = 0;

	// 		deltaT += (leftT - localT) * Conductivity;
	// 		deltaT += (rightT - localT) * Conductivity;
	// 		deltaT += (upT - localT) * Conductivity;
	// 		deltaT += (downT - localT) * Conductivity;

	// 		cell.Temperature += deltaT;
	// 	}
	// }

	// public void CalculateOld(float Conductivity)
	// {
	// 	if (toward == Toward.Up)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[(CellResolution - 1) - j, 0].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[j, 0].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[i, 0].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[(CellResolution - 1) - i, 0].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}
	// 	}
	// 	else if (toward == Toward.Down)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[j, CellResolution - 1].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[(CellResolution - 1) - j, CellResolution - 1].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[(CellResolution - 1) - i, CellResolution - 1].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[i, CellResolution - 1].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}

	// 	}
	// 	else if (toward == Toward.Left)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[0, (CellResolution - 1) - i].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[0, i].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}
	// 	}
	// 	else if (toward == Toward.Right)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[CellResolution - 1, i].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[CellResolution - 1, (CellResolution - 1) - i].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}
	// 	}
	// 	else if (toward == Toward.Forward)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[i, 0].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[i, CellResolution - 1].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}
	// 	}
	// 	else if (toward == Toward.Back)
	// 	{
	// 		for (int i = 0; i < CellResolution; i++)
	// 		{
	// 			for (int j = 0; j < CellResolution; j++)
	// 			{
	// 				float localT = Cells[i, j].Temperature;
	// 				float deltaT = 0;
	// 				if (i + 1 < CellResolution)
	// 					deltaT += (Cells[i + 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (RightNeighbor.Cells[0, j].Temperature - localT) * Conductivity;
	// 				if (i - 1 >= 0)
	// 					deltaT += (Cells[i - 1, j].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (LeftNeighbor.Cells[CellResolution - 1, j].Temperature - localT) * Conductivity;
	// 				if (j + 1 < CellResolution)
	// 					deltaT += (Cells[i, j + 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (DownNeighbor.Cells[(CellResolution - 1) - i, CellResolution - 1].Temperature - localT) * Conductivity;
	// 				if (j - 1 >= 0)
	// 					deltaT += (Cells[i, j - 1].Temperature - localT) * Conductivity;
	// 				else
	// 					deltaT += (UpNeighbor.Cells[(CellResolution - 1) - i, 0].Temperature - localT) * Conductivity;

	// 				Cells[i, j].Temperature += deltaT;
	// 			}
	// 		}
	// 	}
	// 	else
	// 	{
	// 		Print("Chunk/Calculate:???你的toward哪去了?");
	// 	}
	// }

}

public struct Toward
{
	public static readonly Vector3 Up = Vector3.Up;
	public static readonly Vector3 Down = Vector3.Down;
	public static readonly Vector3 Left = Vector3.Left;
	public static readonly Vector3 Right = Vector3.Right;
	public static readonly Vector3 Forward = Vector3.Forward;
	public static readonly Vector3 Back = Vector3.Back;
}
