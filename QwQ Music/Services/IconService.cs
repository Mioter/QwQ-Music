using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QwQ_Music.Services;

public static class IconService
{
    public static StreamGeometry GetIcon(string key)
    {
        return (
                Application.Current ?? throw new InvalidOperationException("Get 'Application.Current' result is null")
            ).FindResource(key) as StreamGeometry
            ?? throw new KeyNotFoundException($"Resource '{key}' not found.");
    }
}
