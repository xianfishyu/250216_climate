using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Earth;
using static Tool;


// [Tool]
public partial class SphereGroup : Node3D
{
	[ExportCategory("MeshSettings")]
	[Export] private int Subsurf = 100;
	[Export] private int 半径 = 100;
	// [ExportToolButton("Reset")] private Callable Reset => Callable.From(PlanesReady);

	[Export] private Dictionary<StringName, MeshInstance3D> Planes = [];

	private Dictionary<string, ShaderMaterial> PlanesTexture = [];

	[ExportCategory("ResourceSettngs")]
	[Export] private Timer Timer;
	[Export] private string ComputePath;
	ComputeCalculator calculator;

	[ExportCategory("CellsSettings")]
	[Export(PropertyHint.Range, "32,1024,32,or_greater,or_less")]
	private int SurfaceReso = 128;

	[ExportCategory("TextureSettings")]
	[Export(PropertyHint.Range, "16,1024,16,or_greater,or_less")]
	private int TextureReso = 128;

	[ExportCategory("HeatSettings")]
	[ExportGroup("ColorSettings")]
	[Export] private Color HotColor = new(1, 0, 0);
	[Export] private Color ColdColor = new(0, 0, 1);
	[ExportGroup("TempSettings")]
	[Export] private float HotThreshold = 100;
	[Export] private float ZeroThreshold = 0;
	[Export] private float ColdThreshold = -100;
	public static ColorMpping colorMpping;

	//模拟需要的变量
	private Dictionary<string, Chunk> Chunks = [];
	private Array<CellIndex> CellIndexList = [];



	public override void _Ready()
	{
		ChunkReady();
		TextureReady();
		calculator = new(ComputePath, SurfaceReso, CellIndexList);
		colorMpping = new(HotThreshold, ZeroThreshold, ColdThreshold, HotColor, ColdColor);
		PlanesReady();
		Calculate();
		TextureUpdate();

		Timer.Timeout += Calculate;
		Timer.Timeout += TextureUpdate;
	}

	private void PlanesReady()
	{
		foreach (var plane in Planes.Values)
		{
			if (plane is null)
				Print("SphereGruop/PlanesUpdate:Planes是空值喵");


			Vector3 offset;
			Vector2 Size;
			bool Flip = false;

			PlaneMesh newMesh = plane.Mesh as PlaneMesh;
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
			else if (Planes["Forward"] == plane)
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

	}

	private void TextureReady()
	{
		PlanesTexture.Add("Up", (ShaderMaterial)Planes["Up"].MaterialOverride);
		PlanesTexture.Add("Down", (ShaderMaterial)Planes["Down"].MaterialOverride);
		PlanesTexture.Add("Left", (ShaderMaterial)Planes["Left"].MaterialOverride);
		PlanesTexture.Add("Right", (ShaderMaterial)Planes["Right"].MaterialOverride);
		PlanesTexture.Add("Forward", (ShaderMaterial)Planes["Forward"].MaterialOverride);
		PlanesTexture.Add("Back", (ShaderMaterial)Planes["Back"].MaterialOverride);

	}

	private void TextureUpdate()
	{
		foreach (var chunk in Chunks.Values)
			chunk.TextureResolution = TextureReso;

		PlanesTexture["Up"].SetShaderParameter("texture_albedo", Chunks["Up"].TextureUpdate());
		PlanesTexture["Down"].SetShaderParameter("texture_albedo", Chunks["Down"].TextureUpdate());
		PlanesTexture["Left"].SetShaderParameter("texture_albedo", Chunks["Left"].TextureUpdate());
		PlanesTexture["Right"].SetShaderParameter("texture_albedo", Chunks["Right"].TextureUpdate());
		PlanesTexture["Forward"].SetShaderParameter("texture_albedo", Chunks["Forward"].TextureUpdate());
		PlanesTexture["Back"].SetShaderParameter("texture_albedo", Chunks["Back"].TextureUpdate());
	}

	private void Calculate()
	{
		float[] LocalTList = calculator.ComputeShaderCal();
		for (var i = 0; i < LocalTList.Length; i++)
		{
			CellIndexList[i].temperature = LocalTList[i];
		}
	}

	private void ChunkReady()
	{
		Chunks.Clear();
		CellIndexList.Clear();

		Chunks.Add("Up", new(Toward.Up, SurfaceReso, TextureReso));
		Chunks.Add("Down", new(Toward.Down, SurfaceReso, TextureReso));
		Chunks.Add("Left", new(Toward.Left, SurfaceReso, TextureReso));
		Chunks.Add("Right", new(Toward.Right, SurfaceReso, TextureReso));
		Chunks.Add("Forward", new(Toward.Forward, SurfaceReso, TextureReso));
		Chunks.Add("Back", new(Toward.Back, SurfaceReso, TextureReso));

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


		CellIndexList = SetIndexList(Chunks);
	}

	private static Array<CellIndex> SetIndexList(Dictionary<string, Chunk> chunks)
	{
		Array<Cell2D> cellList = [];
		foreach (Chunk chunk in chunks.Values)
			foreach (Cell2D cell in chunk.Cells)
			{
				cellList.Add(cell);
			}

		Dictionary<Cell2D, int> dic = [];
		for (int i = 0; i < cellList.Count; i++)
		{
			dic[cellList[i]] = i;
		}

		Array<CellIndex> index = [];
		foreach (var cell in cellList)
		{
			float t = cell.Temperature;
			int up = dic[cell.up];
			int down = dic[cell.down];
			int left = dic[cell.left];
			int right = dic[cell.right];

			if (up == -1 || down == -1 || left == -1 || right == -1)
				Print("Controller2D/SetIndexList:为什么你没有获得一个正常的索引,-1");

			index.Add(new CellIndex(t, up, down, left, right, cell));
		}
		return index;
	}

}

public partial class CellIndex : GodotObject
{
	public float temperature
	{
		get { return cell.Temperature; }
		set { cell.Temperature = value; }
	}
	public Cell2D cell;
	public Vector4I index;
	public CellIndex(float t, int up, int down, int left, int right, Cell2D cell)
	{
		this.cell = cell;
		index = new(up, down, left, right);
		temperature = t;
	}
}

public partial class Cell2D : GodotObject
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
		// Temperature = 10;
		Temperature = RandRange(-100, 100);
		// Temperature = -MathF.Abs(GeoCoordinate.X) * 50 + RandRange(-10, 30);
		// Temperature = GeoCoordinate.X * 57.3f;
		// Temperature = GeoCoordinate.Y * 20f;
		// if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.X)) % 20 <= 5)
		// 	Temperature = 100;
		// else if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.Y)) % 10 <= 5)
		// 	Temperature = -100;
		// else
		// 	Temperature = 0;
	}
}

public partial class Chunk : GodotObject
{
	public Vector3 toward;
	public Cell2D[,] Cells;
	public int CellResolution;

	public Chunk LeftNeighbor;
	public Chunk RightNeighbor;
	public Chunk UpNeighbor;
	public Chunk DownNeighbor;

	private Image textureImage;

	public int TextureResolution
	{
		get => field;
		set
		{
			if (value > CellResolution)
			{
				field = CellResolution;
			}

			else
				field = value;
		}
	}

	public Chunk(Vector3 _toward, int _cellResolution, int _textureResolution)
	{
		toward = _toward;
		CellResolution = _cellResolution;
		Cells = new Cell2D[CellResolution, CellResolution];
		TextureResolution = _textureResolution;
		GenerateCells();
	}

	public Texture2D TextureUpdate()
	{

		//显然,这里的取整有bug,不过只要限定纹理是2的倍数就好了,只要不是质数就丢弃不了多少

		if (textureImage == null)
			textureImage = Image.CreateEmpty(TextureResolution, TextureResolution, false, Image.Format.Rgb8);
		if (textureImage.GetSize() != new Vector2I(TextureResolution, TextureResolution))
			textureImage = Image.CreateEmpty(TextureResolution, TextureResolution, false, Image.Format.Rgb8);

		int scaleFactor = (int)MathF.Ceiling(CellResolution / TextureResolution);

		for (var x = 0; x < TextureResolution; x++)
			for (var y = 0; y < TextureResolution; y++)
			{
				float sigmaT = 0;

				for (var i = 0; i < scaleFactor; i++)
					for (var j = 0; j < scaleFactor; j++)
						sigmaT += Cells[x * scaleFactor + i, y * scaleFactor + j].Temperature;

				float newT = sigmaT / scaleFactor / scaleFactor;

				textureImage.SetPixel(x, y, SphereGroup.colorMpping.GetHeatColor_H_OutOfRange(newT));
			}

		return ImageTexture.CreateFromImage(textureImage);
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


