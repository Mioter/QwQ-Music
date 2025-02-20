using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 音频效果处理链构建器
/// </summary>
public class AudioEffectChain(ISampleProvider baseSource)
{
    // 添加字段声明
    private readonly ISampleProvider _originalSource = baseSource ?? throw new ArgumentNullException(nameof(baseSource));
    private ISampleProvider _currentSource = baseSource;
    private readonly List<IAudioEffect> _effects = [];

    /// <summary>
    /// 添加效果到处理链末尾
    /// </summary>
    /// <param name="effect">要添加的效果</param>
    /// <returns>当前处理链实例（支持链式调用）</returns>
    public AudioEffectChain AddEffect(IAudioEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        
        effect.Initialize(_currentSource);
        _currentSource = effect; // 更新当前链末端
        _effects.Add(effect);
        return this;
    }
    

    /// <summary>
    /// 在指定位置插入效果
    /// </summary>
    /// <param name="index">插入位置索引</param>
    /// <param name="effect">要插入的效果</param>
    /// <returns>当前处理链实例（支持链式调用）</returns>
    public AudioEffectChain InsertEffect(int index, IAudioEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (index < 0 || index > _effects.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_effects.Count == 0)
        {
            return AddEffect(effect);
        }

        // 查找插入位置的前驱节点
        var predecessor = index > 0 ? _effects[index - 1] : _originalSource;
        
        // 初始化新效果
        effect.Initialize(predecessor as IAudioEffect ?? predecessor);

        // 更新后继节点
        if (index < _effects.Count)
        {
            var successor = _effects[index];
            successor.Initialize(effect); // 重新初始化后继
        }

        _effects.Insert(index, effect);
        
        // 重建链式关系
        RebuildChain();
        return this;
    }

    /// <summary>
    /// 移除指定名称的效果
    /// </summary>
    /// <param name="effectName">效果名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveEffect(string effectName)
    {
        var effectToRemove = _effects.FirstOrDefault(e => e.Name == effectName);
        if (effectToRemove == null) return false;

        int index = _effects.IndexOf(effectToRemove);
        _effects.RemoveAt(index);

        // 重建受影响部分的链
        if (index > 0 && index < _effects.Count)
        {
            var prevEffect = _effects[index - 1];
            var nextEffect = _effects[index];
            nextEffect.Initialize(prevEffect);
        }

        RebuildChain();
        return true;
    }
    
    /// <summary>
    /// 移除指定类型的所有效果
    /// </summary>
    /// <typeparam name="T">要移除的效果类型</typeparam>
    /// <returns>实际移除的效果数量</returns>
    public int RemoveEffects<T>() where T : IAudioEffect
    {
        // 找出所有匹配类型的索引（逆序避免索引错位）
        var indices = _effects
            .Select((eff, i) => (eff, i))
            .Where(x => x.eff is T)
            .OrderByDescending(x => x.i)
            .ToList();

        // 逆序移除以保证索引正确
        foreach (var item in indices)
        {
            _effects.RemoveAt(item.i);
        }

        if (indices.Count > 0)
        {
            RebuildChain();
        }
        
        return indices.Count;
    }

    /// <summary>
    /// 移除指定类型的第一个匹配效果
    /// </summary>
    /// <typeparam name="T">要移除的效果类型</typeparam>
    /// <returns>是否成功移除</returns>
    public bool RemoveFirstEffect<T>() where T : IAudioEffect
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            if (_effects[i] is not T) continue;
            _effects.RemoveAt(i);
            RebuildChain();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 移除指定类型的最后一个匹配效果
    /// </summary>
    /// <typeparam name="T">要移除的效果类型</typeparam>
    /// <returns>是否成功移除</returns>
    public bool RemoveLastEffect<T>() where T : IAudioEffect
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            if (_effects[i] is not T) continue;
            _effects.RemoveAt(i);
            RebuildChain();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 链式关系重建方法
    /// </summary>
    private void RebuildChain()
    {
        _currentSource = _originalSource;
        foreach (var effect in _effects)
        {
            effect.Initialize(_currentSource);
            _currentSource = effect;
        }
    }

    /// <summary>
    /// 获取处理链的最终输出源
    /// </summary>
    /// <returns>最终输出源</returns>
    public ISampleProvider GetOutput() => _currentSource;

    /// <summary>
    /// 获取指定名称的效果（支持类型过滤）
    /// </summary>
    /// <typeparam name="T">效果类型</typeparam>
    /// <param name="name">效果名称</param>
    /// <returns>匹配的效果实例，或 null</returns>
    public T? GetEffect<T>(string name) where T : class, IAudioEffect
    {
        return _effects.OfType<T>().FirstOrDefault(e => e.Name == name);
    }

    /// <summary>
    /// 获取全部效果
    /// </summary>
    /// <returns></returns>
    public List<IAudioEffect> GetEffects()
    {
        return _effects;
    }
}