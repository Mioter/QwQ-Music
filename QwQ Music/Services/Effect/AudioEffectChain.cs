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
    private ISampleProvider _source = baseSource; // 当前链的输出源
    private readonly List<IAudioEffect> _effects = []; // 效果列表

    /// <summary>
    /// 添加效果到处理链末尾
    /// </summary>
    /// <param name="effect">要添加的效果</param>
    /// <returns>当前处理链实例（支持链式调用）</returns>
    public AudioEffectChain AddEffect(IAudioEffect effect)
    {
        if (effect == null) throw new ArgumentNullException(nameof(effect));

        effect.Source = _source;
        _source = effect;
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
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        if (index < 0 || index > _effects.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");

        // 插入效果并调整链式结构
        if (_effects.Count == 0)
        {
            // 如果链为空，直接添加效果
            AddEffect(effect);
        }
        else
        {
            // 获取插入位置的前一个效果和后一个效果
            var previousEffect = index > 0 ? _effects[index - 1] : null;
            var nextEffect = index < _effects.Count ? _effects[index] : null;

            // 设置新效果的 Source
            effect.Source = previousEffect?.Source ?? _source;

            // 更新前一个效果的 Source（如果有）
            if (previousEffect != null)
            {
                previousEffect.Source = effect;
            }

            // 更新后一个效果的 Source（如果有）
            if (nextEffect != null)
            {
                nextEffect.Source = effect;
            }

            // 插入效果到列表
            _effects.Insert(index, effect);

            // 更新当前输出源
            _source = _effects.Last();
        }

        return this;
    }

    /// <summary>
    /// 移除指定名称的效果
    /// </summary>
    /// <param name="effectName">效果名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveEffect(string effectName)
    {
        var effectToRemove = _effects.LastOrDefault(e => e.Name == effectName);
        if (effectToRemove == null) return false;

        // 移除效果并调整链式结构
        _effects.Remove(effectToRemove);

        // 如果移除的是第一个效果，则需要重新设置基础源
        if (_effects.Count == 0)
        {
            _source = _source; // 恢复为初始源
        }
        else
        {
            // 找到被移除效果的前一个效果
            var previousEffect = _effects.LastOrDefault(e => e.Source == effectToRemove.Source);
            if (previousEffect != null)
            {
                previousEffect.Source = effectToRemove.Source;
            }

            // 更新当前输出源
            _source = _effects.Last();
        }

        return true;
    }

    /// <summary>
    /// 获取处理链的最终输出源
    /// </summary>
    /// <returns>最终输出源</returns>
    public ISampleProvider GetOutput() => _source;

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
}