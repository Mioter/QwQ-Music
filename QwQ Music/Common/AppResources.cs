using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Common;

public class AppResources : ObservableObject
{
    public static AppResources Default { get; } = new();

    public const string DEFAULT_FONT_KEY = "默认";

    // 使用 Lazy 延迟加载系统字体，提高性能
    private readonly Lazy<Dictionary<string, FontFamily>> _systemFontsLazy;
    
    // 字体集合的只读属性
    public IReadOnlyDictionary<string, FontFamily> CustomFonts { get; } = new Dictionary<string, FontFamily>
    {
        [DEFAULT_FONT_KEY] = FontFamily.Default,
        ["CJTW85"] = new("resm:QwQ_Music.Assets.EmbeddedRes.Fonts.CJTW85.ttf#公众号-犬神志"),
    };

    public IReadOnlyDictionary<string, FontFamily> SystemFonts => _systemFontsLazy.Value;
    
    public string CurrentFont
    {
        get => ConfigManager.UiConfig.ThemeConfig.CurrentFont;
        set => SetCurrentFont(value);
    }

    public AppResources()
    {
        _systemFontsLazy = new Lazy<Dictionary<string, FontFamily>>(() =>
            FontManager.Current.SystemFonts.ToDictionary(x => x.Name, x => x));
    }

    public void Initialize()
    {
        // 尝试设置当前字体，如果失败则使用默认字体
        if (!TrySetFontFromName(CurrentFont))
        {
            CurrentFont = CustomFonts.Keys.First();
        }
    }

    private void SetCurrentFont(string? fontName)
    {
        if (fontName == ConfigManager.UiConfig.ThemeConfig.CurrentFont || !TrySetFontFromName(fontName))
            return;
        
        ConfigManager.UiConfig.ThemeConfig.CurrentFont = fontName ?? DEFAULT_FONT_KEY;
        OnPropertyChanged(nameof(CurrentFont));
    }

    private bool TrySetFontFromName(string? fontName)
    {
        if (string.IsNullOrEmpty(fontName))
            return false;

        return CustomFonts.TryGetValue(fontName, out var customFont) 
            ? SetAppFont(customFont)
            : SystemFonts.TryGetValue(fontName, out var systemFont) && SetAppFont(systemFont);
    }

    /// <summary>
    /// 设置应用程序的字体
    /// </summary>
    /// <param name="font">字体</param>
    /// <returns>true 成功；false 失败</returns>
    public static bool SetAppFont(FontFamily font)
    {
        return ResourceAccessor.Set("AppFont", font);
    }

    /// <summary>
    /// 提供获取所有可用字体名称的方法
    /// </summary>
    /// <returns>字体名称</returns>
    public IEnumerable<string> GetAllFontNames()
    {
        return CustomFonts.Keys.Concat(SystemFonts.Keys);
    }

    /// <summary>
    /// 提供字体存在性检查方法
    /// </summary>
    /// <returns>true 存在；false 不存在</returns>
    public bool ContainsFont(string fontName)
    {
        return CustomFonts.ContainsKey(fontName) || SystemFonts.ContainsKey(fontName);
    }

    /// <summary>
    /// 从已加载字体中查找字体，先后查找自定义字体与系统字体
    /// </summary>
    /// <param name="fontName">字体名</param>
    /// <param name="fontFamily">字体</param>
    /// <returns>true 存在；false 不存在或为<see cref="fontName"/>为null；null 未找到或结果为null</returns>
    public bool TryGetFont(string fontName, out FontFamily? fontFamily)
    {
        // 先尝试从自定义字体中获取
        if (CustomFonts.TryGetValue(fontName, out fontFamily))
            return true;

        // 如果自定义字体中没有，则尝试从系统字体中获取
        if (SystemFonts.TryGetValue(fontName, out fontFamily))
            return true;

        // 都没有找到，返回 false，并将 fontFamily 设置为 null
        fontFamily = null;
        return false;
    }
    
    /// <summary>
    /// 从已加载字体中查找字体，先后查找自定义字体与系统字体
    /// </summary>
    /// <param name="fontName">字体名</param>
    /// <returns>如果字体存在且无错误，则返回此字体，否则返回默认</returns>
    public FontFamily GetFontOrDefault(string fontName)
    {
        // 如果输入为空，直接返回默认字体
        if (!TryGetFont(fontName, out var font)) 
            return FontFamily.Default;

        return font == null ? FontFamily.Default : font;
    }
}