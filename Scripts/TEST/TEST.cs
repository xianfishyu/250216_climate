using Godot;
using System;
using Godot.Collections;
using static Godot.GD;
using static Earth;
using static Tool;

namespace TEST
{
    enum SurfaceType
    {
        LAND,
        OCEAN
    }

    class ComputeShaderInstance
    {

        private RenderingDevice RD;
        private Rid ComputeShader;
        private Rid ComputePipeline;
        private Rid UniformSet;

        
    }
}
