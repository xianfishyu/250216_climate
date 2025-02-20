using Godot;
using System;
using System.ComponentModel;
using static Godot.GD;

public partial class Controller : Node
{
    [Export] PackedScene cellScene;
    MeshInstance3D cellPrefab;

    [Export] float cellSize = 0.1f;
    [Export] int cellsCount = 1000;
    Cell[,] cells;

    public override void _Ready()
    {
        cellPrefab = cellScene.Instantiate<MeshInstance3D>();

        cells = new Cell[cellsCount, cellsCount];
        CreateCells();
    }

    public override void _Process(double delta)
    {
        double alpha = 1e-4;
        double dx = 1.0 / (cellsCount - 1);

        // 辅助函数，用于计算温度分布的导数
        double[,] ComputeHeatEquation(Cell[,] cells, int N, double dx, double alpha)
        {
            double[,] dTdt = new double[N, N];
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    double d2Tdx2 = 0;
                    double d2Tdy2 = 0;

                    if (x > 0) d2Tdx2 += cells[x - 1, y].Temperature;
                    if (x < N - 1) d2Tdx2 += cells[x + 1, y].Temperature;
                    if (y > 0) d2Tdy2 += cells[x, y - 1].Temperature;
                    if (y < N - 1) d2Tdy2 += cells[x, y + 1].Temperature;


                    if (x == 0) d2Tdx2 += cells[N - 1, y].Temperature;
                    if (x == N - 1) d2Tdx2 += cells[0, y].Temperature;
                    if (y == 0) d2Tdy2 += cells[x, N - 1].Temperature;
                    if (y == N - 1) d2Tdy2 += cells[x, 0].Temperature;

                    d2Tdx2 -= 2 * cells[x, y].Temperature;
                    d2Tdy2 -= 2 * cells[x, y].Temperature;

                    dTdt[x, y] = alpha * (d2Tdx2 / (dx * dx) + d2Tdy2 / (dx * dx)); // 将矩阵展平成向量
                }
            }

            return dTdt;
        }

        // https://i.imgur.com/RIlJM32.png
        // https://zhuanlan.zhihu.com/p/8616433050
        double[,] rk4(Cell[,] cells, double dt, int N, double dx, double alpha)
        {
            double[,] T = new double[N, N];

            // TODO 处理边界条件，Dirichlet，Neumann
            // 是不是对完整的体积其实不用处理边界啊懒得写了
            // for (int x = 0; x < N; x++)
            // {
            //     // T[x, 0] = cells[x, 1].Temperature;
            //     // T[x, N - 1] = cells[x, N - 2].Temperature;}
            //     T[x, 0] = 0;
            //     T[x, N - 1] = 0;
            // }
            //
            // for (int y = 0; y < N; y++)
            // {
            //     // T[0, y] = cells[1, y].Temperature;
            //     // T[N - 1, y] = cells[N - 2, y].Temperature;
            //     T[0, y] = 0;
            //     T[N - 1, y] = 0;
            // }


            // 处理内部
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    T[x, y] = cells[x, y].Temperature;
                }
            }

            // 时间积分：使用 Runge-Kutta 方法
            // 计算k1234
            double[,] k1 = ComputeHeatEquation(cells, N, dx, alpha);
            double[,] k2 = ComputeHeatEquation(cells, N, dx, alpha);
            double[,] k3 = ComputeHeatEquation(cells, N, dx, alpha);
            double[,] k4 = ComputeHeatEquation(cells, N, dx, alpha);

            // 更新u_i^(n+1)
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    T[x, y] += dt / 6 * (k1[x, y] + 2 * k2[x, y] + 2 * k3[x, y] + k4[x, y]);
                }
            }

            return T;
        }

        // 数学逼提醒了我用龙格库塔法求偏微分，让我们赞美数学逼
        Cell[,] CellsUpdate = cells;
        double[,] T_new = rk4(cells, delta, cellsCount, dx, alpha);
        for (int x = 0; x < cellsCount; x++)
        {
            for (int y = 0; y < cellsCount; y++)
            {
                CellsUpdate[x, y].Temperature = (float)T_new[x, y];
            }
        }

        cells = CellsUpdate;
        if (Randf() < 0.1)
            cells[RandRange(0, cellsCount - 1), RandRange(0, cellsCount - 1)].Temperature = RandRange(-10, 255);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.R)
            {
                ClearCells();
            }
        }
    }

    private void CreateCells()
    {
        for (int x = 0; x < cellsCount; x++)
        {
            for (int y = 0; y < cellsCount; y++)
            {
                MeshInstance3D cell = cellPrefab.Duplicate() as MeshInstance3D;
                AddChild(cell);
                cell.Scale = new Vector3(cellSize, cellSize, cellSize);
                cell.Position = new Vector3(x * cellSize, 0, y * cellSize);
                cells[x, y] = cell as Cell;
            }
        }
    }

    private void ClearCells()
    {
        for (int x = 0; x < cellsCount; x++)
        {
            for (int y = 0; y < cellsCount; y++)
            {
                cells[x, y].Temperature = 0;
            }
        }
    }
}