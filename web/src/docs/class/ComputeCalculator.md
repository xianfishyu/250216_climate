# ComputeCalculator

这是一个叫Template的类, 里面有什么呢
反正新建类先复制我然后改名?

## 成员

### 成员变量

``` c#
    // 不知道, 你写解释
    private RenderingDevice RD;

    // 
    private Rid ComputeShader;


    private Rid ComputePipeline;


    private Rid UniformSet;


    private Rid LocalTBuffer;


    private Rid CellIndexBuffer;


    private int SurfaceResolution;


    private uint GroupSize;


    private float[] LocalTList;
```

### 成员函数

#### ComputeCalculator
``` c#
// 在什么地方使用
public ComputeCalculator(string path, int resolution, Array<CellIndex> cellIndexList)
    {
        // path做什么, resultuion做什么, cellindex做什么
    }
```

#### ComputeShaderCal
``` c#
//
public float[] ComputeShaderCal()
    {

    }
```


## 用法


### 实例化

``` c#
ComputeCalculator calculator;

calculator = new(ComputePath, SurfaceReso, CellIndexList);

```

### 做什么的

``` c#
float[] LocalTList = calculator.ComputeShaderCal();

```