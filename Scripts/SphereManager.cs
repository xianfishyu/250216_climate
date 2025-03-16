using Godot;
using System;
using static Godot.GD;
using static Tool;

public partial class SphereManager : MeshInstance3D
{
    [ExportCategory("ComputeShaderSettings")]
    [Export] private string ComputeShaderPath;
    private ComputeShaderInstance computeShader;


    [ExportCategory("GridSettings")]
    [Export(PropertyHint.Range, "32,1024,32,or_greater,or_less")]
    private int GridResolution = 128;
    // [Export] private float Conductivity = 0.1f;

    // [ExportCategory("TextureSettings")]
    // [Export] private PackedScene ChunkPrefab;
    // private TextureRect TextureRect;
    // [Export] private float RectSize = 100f;
    // [Export(PropertyHint.Range, "16,1024,16,or_greater,or_less")]
    // private int TextureResolution = 128;

    [Export] private Timer Timer;

    [ExportCategory("HeatSettings")]
    [ExportGroup("ColorSettings")]
    [Export] private Color ColdColor = new(0, 0, 1);
    [Export] private Color HotColor = new(1, 0, 0);
    [ExportGroup("TempSettings")]
    [Export] private float ColdThreshold = -100;
    [Export] private float ZeroThreshold = 0;
    [Export] private float HotThreshold = 100;



    private void 你需要写一个球谐函数()
    {

    }
}
