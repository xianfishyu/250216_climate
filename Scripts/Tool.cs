using System;
using Godot;
using static Godot.GD;





public static class Tool
{
    public static float ZeroThreshold = 0;
    public static Color HotColor = Colors.Red;
    public static Color ColdColor = Colors.Blue;


    public static Color GetHeatColor_H_OutOfRange(float temperature, float top, float bottom)
    {
        float range = Mathf.Abs(top - bottom);
        float t = Mathf.Abs(temperature - bottom) / range * (240f / 359f);
        Color color = new(1, 0, 0);
        color.H = t;
        return color;
    }

    public static Color GetHeatColor_H(float temperature, float top, float bottom)
    {
        if (temperature < bottom)
            temperature = bottom;
        else if (temperature > top)
            temperature = top;
        float range = Mathf.Abs(top - bottom);
        float t = Mathf.Abs(temperature - top) / range * (240f / 359f);
        Color color = new(1, 0, 0);
        color.H = t;
        return color;
    }

    public static Color GetHeatColor_S(float temperature, float top, float bottom)
    {
        if (temperature < bottom)
            return ColdColor;
        else if (temperature < ZeroThreshold)
        {
            float t = Mathf.Abs(temperature - bottom) / Mathf.Abs(ZeroThreshold - bottom);
            Color color = ColdColor;
            color.S = 1 - t;
            return color;
        }
        else if (temperature == ZeroThreshold)
            return Colors.White;
        else if (temperature < top)
        {
            float t = Mathf.Abs(temperature - top) / Mathf.Abs(ZeroThreshold - top);
            Color color = HotColor;
            color.S = 1 - t;
            return color;
        }
        else
            return HotColor;
    }

}