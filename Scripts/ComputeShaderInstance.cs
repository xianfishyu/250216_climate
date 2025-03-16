using Godot;
using System;
using static Godot.GD;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 这应该是一个被封装好的计算着色器
/// 使用构造函数生成一个实例
/// 使用 SetBuffer 来输入数据
/// 使用 InitializeComplete 来告知初始化完成
/// 使用 Calculate 来进行计算
/// 
/// TODOLIST:正确的uniform type设置,类型化的输出,纹理采样
/// </summary>
public class ComputeShaderInstance
{

    private RenderingDevice RD;
    private Rid ComputeShader;
    private Rid ComputePipeline;

    private Dictionary<(uint set, int binding), Rid> Buffers = [];
    private Dictionary<Rid, RenderingDevice.UniformType> UniformType = [];
    private Dictionary<uint, Rid> UniformSet = [];

    private byte[] PushConstant = [];

    /// <summary>
    /// 构造函数,用于生成实例
    /// </summary>
    /// <param name="path">计算着色器的路径</param>
    /// <returns></returns>
    public ComputeShaderInstance(string path) => InitializeShader(path);

    /// <summary>
    /// 初始化计算着色器
    /// </summary>
    /// <param name="path">计算着色器的路径</param>
    private void InitializeShader(string path)
    {
        //加载着色器
        // RD = RenderingServer.CreateLocalRenderingDevice();
        RD = RenderingServer.GetRenderingDevice();

        RDShaderFile ComputeShaderFile = Load<RDShaderFile>(path);
        if (ComputeShaderFile == null)
            throw new ArgumentException($"ComputeShaderInstance/InitializeShader: 无法加载 ComputeShader, 文件路径: {path}");

        RDShaderSpirV shaderBytecode = ComputeShaderFile.GetSpirV();
        ComputeShader = RD.ShaderCreateFromSpirV(shaderBytecode);

        ComputePipeline = RD.ComputePipelineCreate(ComputeShader);
    }

    /// <summary>
    /// 你应该通过调用多次这个函数来输入数据
    /// </summary>
    /// <param name="data">这就是数据本身了</param>
    /// <param name="set">Uniform Set</param>
    /// <param name="binding">Uniform Binding</param>
    /// <typeparam name="T"></typeparam>
    public void SetBuffer<T>(T data, uint set, int binding)
    {
        byte[] bytes = Tool.ConvertToByteArray(data);
        var rid = RD.StorageBufferCreate((uint)bytes.Length, bytes);
        GD.Print("Output: ", string.Join(", ", rid));
        UniformType[rid] = RenderingDevice.UniformType.StorageBuffer;
        Buffers.Add((set, binding), rid);
        // Buffers.Add((set, binding), RD.StorageBufferCreate((uint)bytes.Length, bytes));
    }

    /// <summary>
    /// 更新现有缓冲区内容
    /// </summary>
    public void UpdateBuffer<T>(T data, uint set, int binding)
    {
        if (binding < 0 || binding >= Buffers.Count)
            throw new IndexOutOfRangeException($"无效的缓冲区索引: {binding}");

        Rid buffer = Buffers[(set, binding)];
        byte[] bytes = Tool.ConvertToByteArray(data);

        // 确保GPU操作完成

        RD.BufferUpdate(
            buffer: buffer,
            offset: 0,
            sizeBytes: (uint)bytes.Length,
            data: bytes
        );
        // RD.Submit();
        // RD.Sync();
    }

    /// <summary>
    /// 你可以通过这个函数来输入推式常量
    /// 我还是没有测试过,嘻嘻
    /// 实际上我也不知道最大可以输入多少,但我觉得应该是128byte
    /// </summary>
    /// <param name="objects">输入的数据</param>
    /// <typeparam name="T">匹配: unmanaged</typeparam>
    public void SetPushConstant<T>(params T[] objects) where T : unmanaged
    {
        byte[] bytes = Tool.ConvertToByteArray(objects);
        if (bytes.Length > 0 && bytes.Length <= 128)
        {
            var len = bytes.Length / 4 < 4 ? 4 : bytes.Length / 4;
            byte[] completion = new byte[len];
            for (var i = 0; i < len; i++)
            {
                if (i < bytes.Length)
                    completion[i] = bytes[i];
                else
                    completion[i] = 0;
            }
            PushConstant = PushConstant.Concat(completion).ToArray();
        }
        else
            Print($"ComputeShaderInstance/SetPushConstant:那你输进来的东西超过推式常量的限制,到底输入的是个啥呢: {objects} 还有转换后的东西: {bytes}");
    }

    /// <summary>
    /// 清空你输入的推式常量
    /// </summary>
    public void ClearPushConstant() => PushConstant = [];

    public void SetUniform()
    {

    }

    /// <summary>
    /// 设置纹理 Uniform
    /// </summary>
    /// <param name="textureRid">纹理的 RID</param>
    /// <param name="set">Uniform Set</param>
    /// <param name="binding">Uniform Binding</param>
    public void SetTextureUniform(Rid textureRid, uint set, int binding)
    {
        UniformType[textureRid] = RenderingDevice.UniformType.Image;
        // Buffers.Add((set, binding), textureRid);
        Buffers[(set, binding)] = textureRid;
    }

    /// <summary>
    /// 当输入完所有输入数据后调用这个写入GPU
    /// </summary>
    public void InitializeComplete()
    {
        Dictionary<uint, Dictionary<int, RDUniform>> uniforms = [];
        foreach (var buffer in Buffers)
        {
            uint set = buffer.Key.set;
            int binding = buffer.Key.binding;

            if (!uniforms.ContainsKey(set))
                uniforms[set] = [];

            RDUniform rdUniform = new RDUniform
            {
                UniformType = UniformType[buffer.Value],
                Binding = binding,
            };

            uniforms[set][binding] = rdUniform;
            uniforms[set][binding].AddId(buffer.Value);
        }

        foreach (var kvp in uniforms)
        {
            uint setKey = kvp.Key;
            // 排序后生成数组，确保 Binding 顺序一致
            Godot.Collections.Array<RDUniform> uniformArray = new Godot.Collections.Array<RDUniform>(
                kvp.Value.OrderBy(p => p.Key).Select(p => p.Value).ToArray()
            );

            UniformSet[setKey] = RD.UniformSetCreate(uniformArray, ComputeShader, setKey);
        }
    }

    /// <summary>
    /// 运行计算,请确保工作组与线程组符合数据规模
    /// </summary>
    /// <param name="GroupSizeX">工作组X</param>
    /// <param name="GroupSizeY">工作组Y</param>
    /// <param name="GroupSizeZ">工作组Z</param>
    public void Calculate(uint GroupSizeX, uint GroupSizeY, uint GroupSizeZ)
    {
        long computeList = RD.ComputeListBegin();
        RD.ComputeListBindComputePipeline(computeList, ComputePipeline);

        foreach (var item in UniformSet)
            RD.ComputeListBindUniformSet(computeList, item.Value, item.Key);

        // if (PushConstant.Length > 0)
        //     RD.ComputeListSetPushConstant(computeList, PushConstant, (uint)PushConstant.Length);

        if (PushConstant.Length > 0)
        {
            if (PushConstant.Length > 0 && PushConstant.Length <= 128)
            {
                byte[] completion = new byte[16];
                for (var i = 0; i < 16; i++)
                {
                    if (i < PushConstant.Length)
                        completion[i] = PushConstant[i];
                    else
                        completion[i] = 0;
                }
                PushConstant = completion;
            }

            RD.ComputeListSetPushConstant(computeList, PushConstant, 16);
        }

        RD.ComputeListDispatch(computeList, GroupSizeX, GroupSizeY, GroupSizeZ);
        RD.ComputeListEnd();
        RD.Submit();
        RD.Sync();
    }

    public float[] GetFloatArrayResult(uint set, int binding)
    {
        var outputBytes = RD.BufferGetData(Buffers[(set, binding)]);
        float[] result = new float[outputBytes.Length / sizeof(float)];
        Buffer.BlockCopy(outputBytes, 0, result, 0, outputBytes.Length);
        return result;
    }

}

