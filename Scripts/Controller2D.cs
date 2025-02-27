using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.GD;

// [Tool]
public partial class Controller2D : Node2D
{
	private Dictionary<string, Chunk> Chunks = new();
	private List<CellIndex> CellIndexList = new();
	private float[] LocalTList;

	[ExportCategory("ComputeShaderSettings")]
	[Export] private string ComputeShaderPath;
	private RenderingDevice RD;
	private Rid ComputeShader;
	private Rid ComputePipeline;
	private Rid UniformSet;
	private Rid LocalTBuffer;
	private Rid CellIndexBuffer;


	[ExportCategory("CellsSettings")]
	[Export(PropertyHint.Range, "30,1200,30,or_greater,or_less")] private int CellResolution = 120;
	[Export] private float Conductivity = 0.1f;

	[ExportCategory("TextureSettings")]
	[Export] private PackedScene ChunkPrefab;
	private TextureRect TextureRect;
	[Export] private float CellSize = 1f;
	private Image Image;

	[Export] private Timer Timer;

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
		ComputeShaderReady(CellIndexList);

		Timer.Timeout += Calculate;
		Timer.Timeout += TextureUpdate;
		Timer.Timeout += ComputeShaderCal;
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
				ComputeShaderReady(CellIndexList);
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
		float t = Mathf.Abs(temperature - HotThreshold) / range * (240f / 359f);
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

	private void ComputeShaderReady(List<CellIndex> cellIndexList)
	{
		//加载着色器
		RD = RenderingServer.CreateLocalRenderingDevice();

		RDShaderFile ComputeShaderFile = Load<RDShaderFile>(ComputeShaderPath);
		RDShaderSpirV shaderBytecode = ComputeShaderFile.GetSpirV();
		ComputeShader = RD.ShaderCreateFromSpirV(shaderBytecode);

		//初始化数组
		float[] LocalT = new float[cellIndexList.Count];
		Vector4I[] CellIndex = new Vector4I[cellIndexList.Count];

		for (var i = 0; i < cellIndexList.Count; i++)
		{
			LocalT[i] = cellIndexList[i].temperature;
			CellIndex[i] = cellIndexList[i].index;
		}

		//字节化
		LocalTBuffer = CreateFloatBuffer(LocalT);
		CellIndexBuffer = CreateVector4IBuffer(CellIndex);

		Godot.Collections.Array<RDUniform> uniforms =
		[
			new RDUniform
			{
				UniformType = RenderingDevice.UniformType.StorageBuffer,
				Binding = 0,
			},
			new RDUniform
			{
				UniformType = RenderingDevice.UniformType.StorageBuffer,
				Binding = 1,
			},
		];
		uniforms[0].AddId(LocalTBuffer);
		uniforms[1].AddId(CellIndexBuffer);
		UniformSet = RD.UniformSetCreate(uniforms, ComputeShader, 0);

		Rid CreateFloatBuffer(float[] data)
		{
			byte[] bytes = new byte[data.Length * sizeof(float)];
			Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
			return RD.StorageBufferCreate((uint)bytes.Length, bytes);
		}

		Rid CreateVector4IBuffer(Vector4I[] data)
		{
			// 注意：确保Vector4I的内存布局与shader的uvec4一致
			byte[] bytes = new byte[data.Length * 16]; // 每个Vector4I占16字节
													   // Print("Output: ", string.Join(", ", data));
			for (int i = 0; i < data.Length; i++)
			{
				byte[] x = BitConverter.GetBytes(data[i].X);
				// Print("Output: ", string.Join(", ", x));
				byte[] y = BitConverter.GetBytes(data[i].Y);
				byte[] z = BitConverter.GetBytes(data[i].Z);
				byte[] w = BitConverter.GetBytes(data[i].W);

				Buffer.BlockCopy(x, 0, bytes, i * 16 + 0, 4);
				Buffer.BlockCopy(y, 0, bytes, i * 16 + 4, 4);
				Buffer.BlockCopy(z, 0, bytes, i * 16 + 8, 4);
				Buffer.BlockCopy(w, 0, bytes, i * 16 + 12, 4);
			}

			return RD.StorageBufferCreate((uint)bytes.Length, bytes);
		}

	}

	private void ComputeShaderCal()
	{
		// 创建计算管线
		ComputePipeline = RD.ComputePipelineCreate(ComputeShader);
		long computeList = RD.ComputeListBegin();
		RD.ComputeListBindComputePipeline(computeList, ComputePipeline);
		RD.ComputeListBindUniformSet(computeList, UniformSet, 0);
		RD.ComputeListDispatch(computeList, xGroups: (uint)CellResolution / 30, (uint)CellResolution / 30, zGroups: 6);
		RD.ComputeListEnd();
		RD.Submit();
		RD.Sync();

		// 读取内容
		var outputBytes = RD.BufferGetData(LocalTBuffer);
		Buffer.BlockCopy(outputBytes, 0, LocalTList, 0, outputBytes.Length);
		// Print("Output: ", string.Join(", ", LocalTList));
	}

	private void ChunkReady()
	{
		foreach (Chunk chunk in Chunks.Values)
			chunk.textureRect?.QueueFree();
		Chunks.Clear();
		CellIndexList.Clear();

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


		CellIndexList = SetIndexList(Chunks);
		LocalTList = new float[CellIndexList.Count];
		for (var i = 0; i < CellIndexList.Count; i++)
			LocalTList[i] = CellIndexList[i].temperature;
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

	private static List<CellIndex> SetIndexList(Dictionary<string, Chunk> chunks)
	{
		List<Cell2D> cellList = [];
		foreach (Chunk chunk in chunks.Values)
			foreach (Cell2D cell in chunk.Cells)
			{
				cellList.Add(cell);
			}

		Dictionary<Cell2D, int> dic = new(cellList.Count);
		for (int i = 0; i < cellList.Count; i++)
		{
			dic[cellList[i]] = i;
		}

		List<CellIndex> index = new(cellList.Count);
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

	public void Calculate()
	{
		for (var i = 0; i < CellIndexList.Count; i++)
		{
			CellIndexList[i].temperature = LocalTList[i];
		}
	}

	public class CellIndex
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
			// Temperature = 10;
			// Temperature = RandRange(-100, 100);
			Temperature = -MathF.Abs(GeoCoordinate.X) * 50 + RandRange(-10, 30);
			// Temperature = GeoCoordinate.X * 57.3f;
			// Temperature = GeoCoordinate.Y * 20f;
			// if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.X)) % 10 <= 5)
			// 	Temperature = 100;
			// else if (Mathf.RadToDeg(Mathf.Abs(GeoCoordinate.Y)) % 10 <= 5)
			// 	Temperature = -100;
			// else
			// 	Temperature = 0;
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