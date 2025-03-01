using System;
using Godot;
using static Godot.GD;





public static class Tool
{
    
}

public class ColorMpping(float _HotThreshold = 100, float _ZeroThreshold = 0, float _ColdThreshold = -100,
                        Color _HotColor = default, Color _ColdColor = default)
{
    public float HotThreshold = _HotThreshold;
    public float ZeroThreshold = _ZeroThreshold;
    public float ColdThreshold = _ColdThreshold;

    public Color HotColor = _HotColor == default ? Colors.Red : _HotColor;
    public Color ColdColor = _ColdColor == default ? Colors.Blue : _ColdColor;

    public Color GetHeatColor_H_OutOfRange(float temperature)
    {
        float range = Mathf.Abs(HotThreshold - ColdThreshold);
        float t = Mathf.Abs(temperature - HotThreshold) / range * (240f / 359f);
        Color color = new(1, 0, 0);
        color.H = t;
        return color;
    }

    public Color GetHeatColor_H(float temperature)
    {
        if (temperature < ColdThreshold)
            temperature = ColdThreshold;
        else if (temperature > HotThreshold)
            temperature = HotThreshold;
        float range = Mathf.Abs(HotThreshold - ColdThreshold);
        float t = Mathf.Abs(temperature - HotThreshold) / range * (240f / 359f);
        Color color = new(1, 0, 0);
        color.H = t;
        return color;
    }

    public Color GetHeatColor_S(float temperature)
    {
        if (temperature < ColdThreshold)
            return ColdColor;
        else if (temperature < ZeroThreshold)
        {
            float t = Mathf.Abs(temperature - ColdThreshold) / Mathf.Abs(ZeroThreshold - ColdThreshold);
            Color color = ColdColor;
            color.S = 1 - t;
            return color;
        }
        else if (temperature == ZeroThreshold)
            return Colors.White;
        else if (temperature < HotThreshold)
        {
            float t = Mathf.Abs(temperature - HotThreshold) / Mathf.Abs(ZeroThreshold - HotThreshold);
            Color color = HotColor;
            color.S = 1 - t;
            return color;
        }
        else
            return HotColor;
    }

}