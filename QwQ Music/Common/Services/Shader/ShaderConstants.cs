using System;
using System.IO;

namespace QwQ_Music.Common.Services.Shader;

/// <summary>
///     着色器常量定义
/// </summary>
public static class ShaderConstants
{
    /// <summary>
    ///     波浪扭曲着色器（支持自定义颜色）
    /// </summary>
    public static string WaveWarpShader => LoadShaderFromAvaloniaResource("WaveWarpShader.glsl");

    /// <summary>
    ///     从Avalonia资源加载着色器代码
    /// </summary>
    /// <param name="resourceName">资源名称</param>
    /// <returns>着色器代码</returns>
    private static string LoadShaderFromAvaloniaResource(string resourceName)
    {
        try
        {
            var assembly = typeof(ShaderConstants).Assembly;
            using var stream = assembly.GetManifestResourceStream($"QwQ_Music.Assets.EmbeddedRes.Shaders.{resourceName}");

            if (stream == null)
            {
                throw new FileNotFoundException($"无法找到着色器资源: {resourceName}");
            }

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法加载着色器资源: {resourceName}", ex);
        }
    }

    /// <summary>
    ///     获取所有可用的着色器名称
    /// </summary>
    /// <returns>着色器名称列表</returns>
    public static string[] GetAvailableShaders()
    {
        // 注意：Avalonia资源系统不提供直接列出所有资源的方法
        // 这里返回已知的着色器列表
        return ["WaveWarpShader.glsl"];

        // 如果将来有更多着色器，可以手动添加到列表中
        // return new[] { "WaveWarpShader.glsl", "OtherShader.glsl", ... };
    }
}
