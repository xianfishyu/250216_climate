using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Tool;
using System.Collections.Generic;
using System.Linq;


public class ComputeCalculator
{
    private RenderingDevice RD;
    private Rid ComputeShader;
    private Rid ComputePipeline;
    private Rid UniformSet;
    private Rid LocalTBuffer;
    private Rid CellIndexBuffer;

    private int SurfaceResolution;
    private uint GroupSize;

    private float[] LocalTList;

    private ComputeShaderInstance computeShaderInstance;

    // public ComputeCalculator(string path, int resolution, Array<CellIndex> cellIndexList)
    // {
    //     SurfaceResolution = resolution;
    //     LocalTList = new float[cellIndexList.Count];

    //     GroupSize = (uint)SurfaceResolution / 32;
    //     for (var i = 0; i < cellIndexList.Count; i++)
    //         LocalTList[i] = cellIndexList[i].temperature;

    //     //加载着色器
    //     RD = RenderingServer.CreateLocalRenderingDevice();

    //     RDShaderFile ComputeShaderFile = Load<RDShaderFile>(path);
    //     RDShaderSpirV shaderBytecode = ComputeShaderFile.GetSpirV();
    //     ComputeShader = RD.ShaderCreateFromSpirV(shaderBytecode);


    //     //初始化数组
    //     float[] LocalT = new float[cellIndexList.Count];
    //     Vector4I[] CellIndex = new Vector4I[cellIndexList.Count];

    //     for (var i = 0; i < cellIndexList.Count; i++)
    //     {
    //         LocalT[i] = cellIndexList[i].temperature;
    //         CellIndex[i] = cellIndexList[i].index;
    //     }

    //     //字节化
    //     LocalTBuffer = CreateFloatBuffer(LocalT);
    //     CellIndexBuffer = CreateVector4IBuffer(CellIndex);

    //     Array<RDUniform> uniforms =
    //     [
    //         new RDUniform
    //         {
    //             UniformType = RenderingDevice.UniformType.StorageBuffer,
    //             Binding = 0,
    //         },
    //         new RDUniform
    //         {
    //             UniformType = RenderingDevice.UniformType.StorageBuffer,
    //             Binding = 1,
    //         },
    //     ];
    //     uniforms[0].AddId(LocalTBuffer);
    //     uniforms[1].AddId(CellIndexBuffer);
    //     UniformSet = RD.UniformSetCreate(uniforms, ComputeShader, 0);

    //     Rid CreateFloatBuffer(float[] data)
    //     {
    //         byte[] bytes = new byte[data.Length * sizeof(float)];
    //         Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
    //         return RD.StorageBufferCreate((uint)bytes.Length, bytes);
    //     }

    //     Rid CreateVector4IBuffer(Vector4I[] data)
    //     {
    //         // 注意：确保Vector4I的内存布局与shader的uvec4一致
    //         // 每个Vector4I占16字节
    //         byte[] bytes = new byte[data.Length * 16];
    //         for (int i = 0; i < data.Length; i++)
    //         {
    //             byte[] x = BitConverter.GetBytes(data[i].X);
    //             byte[] y = BitConverter.GetBytes(data[i].Y);
    //             byte[] z = BitConverter.GetBytes(data[i].Z);
    //             byte[] w = BitConverter.GetBytes(data[i].W);

    //             Buffer.BlockCopy(x, 0, bytes, i * 16 + 0, 4);
    //             Buffer.BlockCopy(y, 0, bytes, i * 16 + 4, 4);
    //             Buffer.BlockCopy(z, 0, bytes, i * 16 + 8, 4);
    //             Buffer.BlockCopy(w, 0, bytes, i * 16 + 12, 4);
    //         }

    //         return RD.StorageBufferCreate((uint)bytes.Length, bytes);
    //     }

    // }

    // public float[] ComputeShaderCal()
    // {
    //     // 创建计算管线
    //     ComputePipeline = RD.ComputePipelineCreate(ComputeShader);
    //     long computeList = RD.ComputeListBegin();
    //     RD.ComputeListBindComputePipeline(computeList, ComputePipeline);
    //     RD.ComputeListBindUniformSet(computeList, UniformSet, 0);
    //     RD.ComputeListDispatch(computeList, GroupSize, GroupSize, zGroups: 6);
    //     RD.ComputeListEnd();
    //     RD.Submit();
    //     RD.Sync();

    //     // 读取内容
    //     var outputBytes = RD.BufferGetData(LocalTBuffer);
    //     Buffer.BlockCopy(outputBytes, 0, LocalTList, 0, outputBytes.Length);

    //     return LocalTList;
    // }

    public ComputeCalculator(string path, int resolution, Array<CellIndex> cellIndexList)
    {
        SurfaceResolution = resolution;
        LocalTList = new float[cellIndexList.Count];

        GroupSize = (uint)SurfaceResolution / 32;
        for (var i = 0; i < cellIndexList.Count; i++)
            LocalTList[i] = cellIndexList[i].temperature;

        computeShaderInstance = new(path,
        [
            (typeof(float[]),LocalTList),
            (typeof(Vector4I[]),cellIndexList.Select(ci => ci.index).ToArray())
        ]);

    }

    public float[] ComputeShaderCal()
    {
        computeShaderInstance.Calculate(GroupSize,GroupSize,6);

        return computeShaderInstance.GetFloatArrayResult(0);
    }

}