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


    //请限定为偶数
    private void 六面体球生成(int resol)
    {
        //限定偶数
        if (resol % 2 != 0)
        {
            resol++;
            Print("SphereManager/六面体球生成:resol输入不为偶!自动加一" + resol);
        }

        // 生成球
        //+X,-X,+Y,-Y,+Z,-Z
        float radius = resol / 2;
        Pixel[,] sphere = new Pixel[resol * 6, resol];

        //+X
        for (var i = 0; i < resol; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(radius, j - radius, -(i - radius));
                sphere[i, j].CalculateRadian();
            }
        }
        
        //-X
        for (var i = resol; i < resol * 2; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(radius, j - radius, i - radius);
                sphere[i, j].CalculateRadian();
            }
        }

        //+Y
        for (var i = resol * 2; i < resol * 3; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(i - radius, radius, -(j - radius));
                sphere[i, j].CalculateRadian();
            }
        }

        //-Y
        for (var i = resol * 3; i < resol * 4; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(i - radius, radius, j - radius);
                sphere[i, j].CalculateRadian();
            }
        }

        //+Z
        for (var i = resol * 4; i < resol * 5; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(i - radius, j - radius, radius);
                sphere[i, j].CalculateRadian();
            }
        }

        //-Z
        for (var i = resol * 5; i < resol * 6; i++)
        {
            for (var j = 0; j < resol; j++)
            {
                sphere[i, j].UVPos = new Vector2(i / (resol - 1), j / (resol - 1));

                sphere[i, j].CubePos = new Vector3(-(i - radius), j - radius, radius);
                sphere[i, j].CalculateRadian();
            }
        }
        // 生成经纬度
    }

    public struct Pixel
    {
        //经纬坐标,使用弧度制
        public Vector2 SpherePos;
        //矩阵坐标
        public Vector3 CubePos;
        //UV坐标
        public Vector2 UVPos;

        public void CalculateRadian()
        {
            SpherePos = new(MathF.Atan2(CubePos.Y, MathF.Sqrt(MathF.Pow(CubePos.X, 2) + MathF.Pow(CubePos.Z, 2))),
                                            MathF.Atan2(CubePos.X, -CubePos.Z));
        }
    }
}
