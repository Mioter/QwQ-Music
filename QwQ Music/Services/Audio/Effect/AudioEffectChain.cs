using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 支持命名和优先级的音频效果处理链
/// </summary>
public class AudioEffectChain(ISampleProvider baseSource)
{
    private readonly ISampleProvider _baseSource = baseSource ?? throw new ArgumentNullException(nameof(baseSource));
    private readonly SortedList<int, IAudioEffect> _priorityMap = new(); // 优先级→效果器
    private readonly Dictionary<string, IAudioEffect> _nameMap = new(); // 名称→效果器
    private ISampleProvider _currentSource = baseSource;

    /// <summary>
    /// 添加效果器（自动处理名称和优先级冲突）
    /// </summary>
    public AudioEffectChain AddEffect(IAudioEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ValidateName(effect.Name);

        // 自动处理优先级冲突
        while (_priorityMap.ContainsKey(effect.Priority))
            effect.Priority++;

        // 注册效果器
        _priorityMap.Add(effect.Priority, effect);
        _nameMap.Add(effect.Name, effect);

        RebuildChain();
        return this;
    }

    /// <summary>
    /// 通过名称获取效果器
    /// </summary>
    public IAudioEffect? GetEffect(string name)
    {
        return _nameMap.GetValueOrDefault(name);
    }

    /// <summary>
    /// 获取指定的类型与名称的效果器
    /// </summary>
    public T? GetEffect<T>(string name)
        where T : class, IAudioEffect
    {
        if (GetEffect(name) is T effect)
            return effect;
        return null;
    }

    /// <summary>
    /// 通过类型获取所有效果器
    /// </summary>
    public IEnumerable<T> GetEffects<T>()
        where T : class, IAudioEffect
    {
        return _nameMap.Values.OfType<T>();
    }

    /// <summary>
    /// 移除指定名称的效果器
    /// </summary>
    public bool RemoveEffect(string name)
    {
        if (!_nameMap.TryGetValue(name, out var effect))
            return false;

        _priorityMap.Remove(effect.Priority);
        _nameMap.Remove(name);
        RebuildChain();
        return true;
    }

    /// <summary>
    /// 移除所有效果器
    /// </summary>
    public void Clear()
    {
        _priorityMap.Clear();
        _nameMap.Clear();
        _currentSource = _baseSource;
    }

    /// <summary>
    /// 验证名称唯一性
    /// </summary>
    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("效果器名称不能为空");
        if (_nameMap.ContainsKey(name))
            throw new ArgumentException($"名称为'{name}'的效果器已存在");
    }

    /// <summary>
    /// 重建处理链
    /// </summary>
    private void RebuildChain()
    {
        _currentSource = _baseSource;
        foreach (var effect in _priorityMap.Values.OrderBy(e => e.Priority))
        {
            effect.Initialize(_currentSource);
            _currentSource = effect;
        }
    }

    /// <summary>
    /// 获取最终输出源
    /// </summary>
    public ISampleProvider GetOutput() => _currentSource;
}
