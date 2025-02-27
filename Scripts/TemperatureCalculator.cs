using static Godot.GD;
namespace _Climate.Scripts;

public class TemperatureCalculator(int width, int height, double alpha)
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly double Alpha = alpha;
    public double[,] Cells = new double[width, height];
    public double[,] CellsDerivative = new double[width, height];  // 变温分布
    public double[,] CellsAnomaly = new double[width, height];  // 气温距平分布
    private double[,] _cellsAverage = new double[width, height];  // 平均值
    private uint _averageCount = 0;
    
    
    public void Calculate(double delta)
    {
        var dx2 = 1.0 / ((Width - 1) * (Height - 1));

        // 辅助函数，用于计算温度分布的导数
        double[,] ComputeHeatEquation(double[,] cells, double[,] uk, double uk_delta, int width, int height, double dx2, double alpha)
        {
            uk ??= new double[width, height];
            double[,] dTdt = new double[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double d2Tdx2 = 0;
                    double d2Tdy2 = 0;
                    

                    if (x > 0) d2Tdx2 += cells[x - 1, y] + uk[x - 1, y] * uk_delta;
                    if (x < width - 1) d2Tdx2 += cells[x + 1, y] + uk[x + 1, y] * uk_delta;
                    if (y > 0) d2Tdy2 += cells[x, y - 1] + uk[x, y - 1] * uk_delta;
                    if (y < height - 1) d2Tdy2 += cells[x, y + 1] + uk[x, y + 1] * uk_delta;


                    if (x == 0) d2Tdx2 += cells[width - 1, y] + uk[width - 1, y] * uk_delta;
                    if (x == width - 1) d2Tdx2 += cells[0, y] + uk[0, y] * uk_delta;
                    if (y == 0) d2Tdy2 += cells[x, height - 1] + uk[x, height - 1] * uk_delta;
                    if (y == height - 1) d2Tdy2 += cells[x, 0] + uk[x, 0] * uk_delta;

                    d2Tdx2 -= 2 * (cells[x, y] + uk[x, y] * uk_delta);
                    d2Tdy2 -= 2 * (cells[x, y] + uk[x, y] * uk_delta);
                    

                    dTdt[x, y] = alpha * (d2Tdx2 / (dx2) + d2Tdy2 / (dx2)); // 将矩阵展平成向量
                }
            }

            return dTdt;
        }

        // https://i.imgur.com/RIlJM32.png
        // https://zhuanlan.zhihu.com/p/8616433050
        double[,] rk4(double[,] cells, double dt, int width, int height, double dx2, double alpha)
        {
            var T = new double[width, height];

            // 处理内部
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    T[x, y] = cells[x, y];
                }
            }

            // 时间积分：使用 Runge-Kutta 方法
            // 计算k1234
            var k1 = ComputeHeatEquation(cells, null, 0, width, height, dx2, alpha);
            var k2 = ComputeHeatEquation(cells, k1, delta / 2, width, height, dx2, alpha);
            var k3 = ComputeHeatEquation(cells, k2, delta / 2, width, height, dx2, alpha);
            var k4 = ComputeHeatEquation(cells, k3, delta, width, height, dx2, alpha);

            // 更新u_i^(n+1)
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    T[x, y] += dt / 6 * (k1[x, y] + 2 * k2[x, y] + 2 * k3[x, y] + k4[x, y]);
                }
            }

            return T;
        }

        // 数学逼提醒了我用龙格库塔法求偏微分，让我们赞美数学逼
        var cellsUpdate = Cells;
        var tNew = rk4(Cells, delta, Width, Height, dx2, Alpha);
        CellsDerivative = ComputeHeatEquation(Cells, null, 0, Width, Height, dx2, Alpha);
        
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                cellsUpdate[x, y] = (float)tNew[x, y];
            }
        }
        
        // 距平值计算
        for (var x = 0; x < Width; x++) 
        {
            for (var y = 0; y < Height; y++)
            {
                _cellsAverage[x, y] += (Cells[x, y] - _cellsAverage[x, y]) / (_averageCount + 1);
            }
        }

        // 单元格的距平值
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                CellsAnomaly[x, y] = Cells[x, y] - _cellsAverage[x, y];
            }
        }
    
        if(_averageCount < 1000)
            _averageCount ++;
        
        Cells = cellsUpdate;
    }
    
    public void ClearCells()
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                Cells[x, y] = 0;
                // CellsDerivative[x, y] = 0;
                CellsAnomaly[x, y] = 0;
                _cellsAverage[x, y] = 0;
            }
        }
    }
}
