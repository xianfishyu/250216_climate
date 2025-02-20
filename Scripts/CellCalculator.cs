using static Godot.GD;
namespace _Climate.Scripts;

public class CellCalculator(int _width, int _height)
{
    private int width = _width;
    private int height = _height;
    public CellForCalculation[,] cells = new CellForCalculation[_width, _height];
    
    public struct CellForCalculation
    {
        public float Temperature
        {
            get
            {
                _temperature ??= RandRange(-10, 50);
                // _temperature ??= 0;
                return (float)_temperature;
            }
            set => _temperature = value;
        }
        private float? _temperature;
    }
    
    public void Calculate(double delta)
    {
        double alpha = 1e-4;
        double dx2 = 1.0 / ((width - 1) * (height - 1));

        // 辅助函数，用于计算温度分布的导数
        double[,] ComputeHeatEquation(CellForCalculation[,] cells, int width, int height, double dx2, double alpha)
        {
            double[,] dTdt = new double[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double d2Tdx2 = 0;
                    double d2Tdy2 = 0;

                    if (x > 0) d2Tdx2 += cells[x - 1, y].Temperature;
                    if (x < width - 1) d2Tdx2 += cells[x + 1, y].Temperature;
                    if (y > 0) d2Tdy2 += cells[x, y - 1].Temperature;
                    if (y < height - 1) d2Tdy2 += cells[x, y + 1].Temperature;


                    if (x == 0) d2Tdx2 += cells[width - 1, y].Temperature;
                    if (x == width - 1) d2Tdx2 += cells[0, y].Temperature;
                    if (y == 0) d2Tdy2 += cells[x, height - 1].Temperature;
                    if (y == height - 1) d2Tdy2 += cells[x, 0].Temperature;

                    d2Tdx2 -= 2 * cells[x, y].Temperature;
                    d2Tdy2 -= 2 * cells[x, y].Temperature;

                    dTdt[x, y] = alpha * (d2Tdx2 / (dx2) + d2Tdy2 / (dx2)); // 将矩阵展平成向量
                }
            }

            return dTdt;
        }

        // https://i.imgur.com/RIlJM32.png
        // https://zhuanlan.zhihu.com/p/8616433050
        double[,] rk4(CellForCalculation[,] cells, double dt, int width, int height, double dx2, double alpha)
        {
            double[,] T = new double[width, height];

            // 处理内部
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    T[x, y] = cells[x, y].Temperature;
                }
            }

            // 时间积分：使用 Runge-Kutta 方法
            // 计算k1234
            double[,] k1 = ComputeHeatEquation(cells, width, height, dx2, alpha);
            double[,] k2 = ComputeHeatEquation(cells, width, height, dx2, alpha);
            double[,] k3 = ComputeHeatEquation(cells, width, height, dx2, alpha);
            double[,] k4 = ComputeHeatEquation(cells, width, height, dx2, alpha);

            // 更新u_i^(n+1)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    T[x, y] += dt / 6 * (k1[x, y] + 2 * k2[x, y] + 2 * k3[x, y] + k4[x, y]);
                }
            }

            return T;
        }

        // 数学逼提醒了我用龙格库塔法求偏微分，让我们赞美数学逼
        CellForCalculation[,] CellsUpdate = cells;
        double[,] T_new = rk4(cells, delta, width, height, dx2, alpha);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellsUpdate[x, y].Temperature = (float)T_new[x, y];
            }
        }

        cells = CellsUpdate;
        if (Randf() < 0.1)
            cells[RandRange(0, width - 1), RandRange(0, height - 1)].Temperature = RandRange(-10, 255);
    }
    
    public void ClearCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y].Temperature = 0;
            }
        }
    }
}
