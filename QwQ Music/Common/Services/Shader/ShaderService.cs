using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace QwQ_Music.Common.Services.Shader;

/// <summary>
///     着色器服务，负责编译和管理着色器
/// </summary>
public class ShaderService {
    private readonly SKShader[]? _children;
    private readonly string _fragmentShader;
    private readonly Stopwatch _timeWatch = Stopwatch.StartNew();
    private SKRuntimeEffect? _effect;
    private int _frameIndex;
    private float _lastElapsedSeconds;

    /// <summary>
    ///     初始化着色器服务
    /// </summary>
    /// <param name="fragmentShader">GLSL片段着色器代码</param>
    /// <param name="children">子着色器（如纹理）</param>
    public ShaderService(string fragmentShader, SKShader[]? children = null) {
        _fragmentShader = fragmentShader;
        _children = children;
        InitializeShader();
    }

    /// <summary>
    ///     着色器颜色列表
    /// </summary>
    public Color[]? Colors { get; set; }

    /// <summary>
    ///     初始化着色器
    /// </summary>
    private void InitializeShader() {
        var result = SKRuntimeEffect.CreateShader(_fragmentShader, out string? errorText);
        _effect = result;

        if (errorText == null)
            return;

        // 记录详细错误
        Console.WriteLine(
            $"""
             着色器初始化错误: {errorText} 

             着色器代码: {_fragmentShader} 

             """);
    }

    /// <summary>
    ///     创建着色器实例
    /// </summary>
    /// <param name="size">渲染尺寸</param>
    /// <param name="mousePosition">鼠标位置</param>
    /// <returns>SkiaSharp着色器对象</returns>
    public SKShader CreateShader(Size size, Vector2? mousePosition = null) {
        if (_effect == null)
            return SKShader.CreateColor(SKColors.Magenta); // 错误状态显示

        var uniforms = new SKRuntimeEffectUniforms(_effect);

        // 设置着色器输入参数
        float timeElapsed = (float)_timeWatch.Elapsed.TotalSeconds;
        float timeDelta = Math.Max(timeElapsed - _lastElapsedSeconds, 1f / 240f);
        _lastElapsedSeconds = timeElapsed;
        _frameIndex++;

        // 修复Vector3转换问题，使用float数组
        float[] resolution = [(float)size.Width, (float)size.Height, 0];
        uniforms["iResolution"] = resolution;

        uniforms["iTime"] = timeElapsed;
        uniforms["iTimeDelta"] = timeDelta;
        uniforms["iFrameRate"] = 1.0f / timeDelta;
        uniforms["iFrame"] = _frameIndex;

        // 修复Vector4转换问题，使用float数组
        float[] mousePos;

        if (mousePosition.HasValue)
            mousePos = [mousePosition.Value.X, mousePosition.Value.Y, 0, 0];
        else
            mousePos = [0, 0, 0, 0];

        uniforms["iMouse"] = mousePos;

        // 添加颜色参数
        if (Colors is { Length: > 0 }) {
            // 最多支持4个颜色
            int colorCount = Math.Min(Colors.Length, 4);

            for (int i = 0; i < colorCount; i++) {
                Color color = Colors[i];
                float[] colorArray = [color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f];
                uniforms[$"iColor{i}"] = colorArray;
            }

            // 传递颜色数量
            uniforms["iColorCount"] = colorCount;
        } else {
            // 默认颜色
            uniforms["iColorCount"] = 0;
        }

        // 创建着色器，修复children参数类型
        SKRuntimeEffectChildren? children = _children != null ? new SKRuntimeEffectChildren(_effect) : null;

        if (children == null || _children == null)
            return _effect.ToShader(uniforms);

        // 修复索引器参数类型，使用字符串索引而不是整数
        for (int i = 0; i < _children.Length && i < _effect.Children.Count; i++) {
            string childName = _effect.Children[i];
            children[childName] = _children[i];
        }

        return _effect.ToShader(uniforms, children);
    }

    /// <summary>
    ///     重置着色器计时器
    /// </summary>
    public void ResetTimer() {
        _timeWatch.Restart();
        _frameIndex = 0;
        _lastElapsedSeconds = 0;
    }
}