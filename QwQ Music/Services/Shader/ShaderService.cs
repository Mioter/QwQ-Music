using System;
using System.Numerics;
using Avalonia;
using SkiaSharp;

namespace QwQ_Music.Services.Shader;

/// <summary>
/// 着色器服务，负责编译和管理着色器
/// </summary>
public class ShaderService
{
    private readonly string _fragmentShader;
    private SKRuntimeEffect? _effect;
    private readonly SKShader[]? _children;
    private DateTime _startTime;

    /// <summary>
    /// 初始化着色器服务
    /// </summary>
    /// <param name="fragmentShader">GLSL片段着色器代码</param>
    /// <param name="children">子着色器（如纹理）</param>
    public ShaderService(string fragmentShader, SKShader[]? children = null)
    {
        _fragmentShader = fragmentShader;
        _children = children;
        _startTime = DateTime.Now;
        InitializeShader();
    }

    /// <summary>
    /// 初始化着色器
    /// </summary>
    private void InitializeShader()
    {
        try
        {
            // 修复方法名：使用正确的方法 CreateShader
            var result = SKRuntimeEffect.CreateShader(_fragmentShader, out string? errorText);
            _effect = result ?? throw new InvalidOperationException($"着色器编译失败: {errorText}");
        }
        catch (Exception ex)
        {
            // 记录错误
            Console.WriteLine($"着色器初始化错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建着色器实例
    /// </summary>
    /// <param name="size">渲染尺寸</param>
    /// <param name="mousePosition">鼠标位置</param>
    /// <returns>SkiaSharp着色器对象</returns>
    public SKShader CreateShader(Size size, Vector2? mousePosition = null)
    {
        if (_effect == null)
            return SKShader.CreateColor(SKColors.Magenta); // 错误状态显示

        var uniforms = new SKRuntimeEffectUniforms(_effect);
            
        // 设置着色器输入参数
        float timeElapsed = (float)(DateTime.Now - _startTime).TotalSeconds;
            
        // 修复Vector3转换问题，使用float数组
        float[] resolution = [(float)size.Width, (float)size.Height, 0];
        uniforms["iResolution"] = resolution;
            
        uniforms["iTime"] = timeElapsed;
        uniforms["iTimeDelta"] = 1.0f / 60.0f; // 假设60fps
        uniforms["iFrameRate"] = 60.0f;
        uniforms["iFrame"] = (int)(timeElapsed * 60);
            
        // 修复Vector4转换问题，使用float数组
        float[] mousePos;
        if (mousePosition.HasValue)
        {
            mousePos =
            [
                mousePosition.Value.X, 
                mousePosition.Value.Y, 
                0, 0,
            ];
        }
        else
        {
            mousePos = [0, 0, 0, 0];
        }
        uniforms["iMouse"] = mousePos;
            
        // 创建着色器，修复children参数类型
        var children = _children != null ? new SKRuntimeEffectChildren(_effect) : null;
        if (children != null && _children != null)
        {
            // 修复索引器参数类型，使用字符串索引而不是整数
            for (int i = 0; i < _children.Length && i < _effect.Children.Count; i++)
            {
                string childName = _effect.Children[i];
                children[childName] = _children[i];
            }
        }
            
        return _effect.ToShader(uniforms, children);
    }
        
    /// <summary>
    /// 重置着色器计时器
    /// </summary>
    public void ResetTimer()
    {
        _startTime = DateTime.Now;
    }
}