using System;
using Avalonia.Media;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;

namespace QwQ_Music.ViewModels;

public class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; set; } = ConfigInfoModel.PlayerConfig;
    

    private readonly MusicPlayerViewModel _musicPlayer = MusicPlayerViewModel.Instance;
    
    private readonly Random _random = new();

    public IBrush RandomColor => GeneratePastelColor();

    private SolidColorBrush GeneratePastelColor()
    {
        // 生成明亮的色相（0-360度）
        double hue = _random.Next(0, 360);

        // 保持高饱和度（70%-100%）
        double saturation = _random.Next(70, 100) / 100.0;

        // 保持较高亮度（70%-90%）
        double value = _random.Next(70, 90) / 100.0;

        Color color = HsvToRgb(hue, saturation, value);
        return new SolidColorBrush(color);
    }

    private static Color HsvToRgb(double hue, double saturation, double value)
    {
        int hi = (int)(hue / 60) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        double v = value * 255;
        byte vByte = (byte)Math.Round(v);
        double p = v * (1 - saturation);
        byte pByte = (byte)Math.Round(p);
        double q = v * (1 - f * saturation);
        byte qByte = (byte)Math.Round(q);
        double t = v * (1 - (1 - f) * saturation);
        byte tByte = (byte)Math.Round(t);

        return hi switch
        {
            0 => Color.FromRgb(vByte, tByte, pByte),
            1 => Color.FromRgb(qByte, vByte, pByte),
            2 => Color.FromRgb(pByte, vByte, tByte),
            3 => Color.FromRgb(pByte, qByte, vByte),
            4 => Color.FromRgb(tByte, pByte, vByte),
            _ => Color.FromRgb(vByte, pByte, qByte),
        };
    }
}
