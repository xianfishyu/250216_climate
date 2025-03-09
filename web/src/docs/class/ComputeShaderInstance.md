# ComputeShaderInstance

包装好的着色计算器实例

## 成员

### 成员变量

```c#
private RenderingDevice RD;
private Rid ComputeShader;
private Rid ComputePipeline;

private List<Rid> Buffers = [];
private Rid UniformSet;
```

### 成员函数

#### Ctor

```c#
public ComputeShaderInstance(string path, List<(Type, object)> inputs)
```

#### Calculate

```c#
public void Calculate(uint GroupSizeX, uint GroupSizeY, uint GroupSizeZ)
```

#### GetResult

```csharp
public float[] GetFloatArrayResult(int id)

public int[] GetIntArrayResult(int id)
```

## 用法

### 实例化

```c#
ComputeShaderInstance calculator;

computeShaderInstance = new(path,
        [
            (typeof(type),Data1),
            (typeof(type),DataList),
            ...)
        ]);

```

### 做什么的

```c#


```
