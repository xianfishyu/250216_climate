using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Earth;
using static Tool;
using System.Collections.Generic;

namespace TEST
{
    enum SurfaceType
    {
        LAND,
        OCEAN
    }

    public class ComputeShaderInstance
    {

        private RenderingDevice RD;
        private Rid ComputeShader;
        private Rid ComputePipeline;

        private List<Rid> Buffers = [];
        private Rid UniformSet;

        //这玩意你还没算
        // private uint GroupSize;


        public ComputeShaderInstance(string path, List<(Type, object)> inputs)
        {
            if (inputs is null)
                Print("ComputeShaderInstance/ctor/inputs:卧槽为啥这个你还能输进来个空值你是人类吗");

            InitializeShader(path);
            InitializeBuffer(inputs);
        }

        private void InitializeBuffer(List<(Type, object)> inputs)
        {
            Array<RDUniform> uniforms = [];
            for (int i = 0; i < inputs.Count; i++)
            {
                switch (inputs[i].Item1)
                {
                    case Type t when t == typeof(float):
                        Buffers.Add(FloatToBuffer((float)inputs[i].Item2));
                        break;
                    case Type t when t == typeof(float[]):
                        Buffers.Add(FloatArrayToBuffer((float[])inputs[i].Item2));
                        break;
                    case Type t when t == typeof(int):
                        Buffers.Add(IntToBuffer((int)inputs[i].Item2));
                        break;
                    case Type t when t == typeof(int[]):
                        Buffers.Add(IntArrayToBuffer((int[])inputs[i].Item2));
                        break;
                    case Type t when t == typeof(Vector4I[]):
                        Buffers.Add(Vector4IArrayToBuffer((Vector4I[])inputs[i].Item2));
                        break;
                    default:
                        throw new ArgumentException($"ComputeShaderInstance/InitializeBuffer:未定义的类型,请写入解释函数喵,类型: {inputs[i].Item1}");
                }

                uniforms.Add(new RDUniform
                {
                    UniformType = RenderingDevice.UniformType.StorageBuffer,
                    Binding = i,
                });
                uniforms[i].AddId(Buffers[i]);

            }

            UniformSet = RD.UniformSetCreate(uniforms, ComputeShader, 0);
        }

        private void InitializeShader(string path)
        {
            //加载着色器
            RD = RenderingServer.CreateLocalRenderingDevice();

            RDShaderFile ComputeShaderFile = Load<RDShaderFile>(path);
            if (ComputeShaderFile == null)
                throw new ArgumentException($"ComputeShaderInstance/InitializeShader: 无法加载 ComputeShader, 文件路径: {path}");

            RDShaderSpirV shaderBytecode = ComputeShaderFile.GetSpirV();
            ComputeShader = RD.ShaderCreateFromSpirV(shaderBytecode);

            ComputePipeline = RD.ComputePipelineCreate(ComputeShader);
        }

        public void Calculate(uint GroupSizeX, uint GroupSizeY, uint GroupSizeZ)
        {
            long computeList = RD.ComputeListBegin();
            RD.ComputeListBindComputePipeline(computeList, ComputePipeline);
            RD.ComputeListBindUniformSet(computeList, UniformSet, 0);
            RD.ComputeListDispatch(computeList, GroupSizeX, GroupSizeY, GroupSizeZ);
            RD.ComputeListEnd();
            RD.Submit();
            RD.Sync();
        }

        //这里写的有问题,请更改
        //更改了,但是对吗?
        public float[] GetFloatArrayResult(int id)
        {
            var outputBytes = RD.BufferGetData(Buffers[id]);
            float[] result = new float[outputBytes.Length / sizeof(float)];
            Buffer.BlockCopy(outputBytes, 0, result, 0, outputBytes.Length);
            return result;
        }

        public int[] GetIntArrayResult(int id)
        {
            var outputBytes = RD.BufferGetData(Buffers[id]);
            int[] result = new int[outputBytes.Length / sizeof(int)];
            Buffer.BlockCopy(outputBytes, 0, result, 0, outputBytes.Length);
            return result;
        }

        private Rid FloatToBuffer(float data)
        {
            byte[] floatBytes = BitConverter.GetBytes(data);
            return RD.StorageBufferCreate((uint)floatBytes.Length, floatBytes);
        }

        private Rid FloatArrayToBuffer(float[] data)
        {
            byte[] bytes = new byte[data.Length * sizeof(float)];
            Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            return RD.StorageBufferCreate((uint)bytes.Length, bytes);
        }

        private Rid IntToBuffer(int data)
        {
            byte[] bytes = new byte[sizeof(int)];
            byte[] intBytes = BitConverter.GetBytes(data);
            Buffer.BlockCopy(intBytes, 0, bytes, 0, intBytes.Length);
            return RD.StorageBufferCreate((uint)bytes.Length, bytes);
        }

        private Rid IntArrayToBuffer(int[] data)
        {
            byte[] bytes = new byte[data.Length * sizeof(int)];
            Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            return RD.StorageBufferCreate((uint)bytes.Length, bytes);
        }

        private Rid Vector4IArrayToBuffer(Vector4I[] data)
        {
            // 注意：确保Vector4I的内存布局与shader的uvec4一致
            // 每个Vector4I占16字节
            byte[] bytes = new byte[data.Length * 16];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(data[i].X);
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
}
