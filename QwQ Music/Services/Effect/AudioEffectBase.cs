using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 基础装饰器实现
/// </summary>
public abstract class AudioEffectBase : IAudioEffect
{
    private readonly Dictionary<string, object?> _parameters = new(); // 用于存储动态参数
    private bool _enabled = true; // 默认启用效果器

    /// <summary>
    /// 音频源
    /// </summary>
    protected ISampleProvider Source { get; private set; } = null!;

    /// <summary>
    /// 效果名称
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 是否启用该效果器
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            OnEnabledChanged();
        }
    }

    /// <summary>
    /// 效果器的优先级，用于确定效果器链中的执行顺序
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 音频格式
    /// </summary>
    public WaveFormat WaveFormat => Source.WaveFormat;

    /// <summary>
    /// 初始化效果器并设置音频源
    /// </summary>
    /// <param name="source">音频源</param>
    public void Initialize(ISampleProvider source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        OnInitialized();
    }

    /// <summary>
    /// 异步初始化效果器并设置音频源
    /// </summary>
    /// <param name="source">音频源</param>
    public virtual Task InitializeAsync(ISampleProvider source)
    {
        Initialize(source);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    public virtual void SetParameter<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("参数名称不能为空", nameof(key));
        _parameters[key] = value;
        OnParameterChanged(key, value);
    }

    /// <summary>
    /// 获取效果器的配置参数
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <returns>参数值</returns>
    public object? GetParameter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("参数名称不能为空", nameof(key));
        return _parameters.GetValueOrDefault(key);
    }

    /// <summary>
    /// 类型安全的获取效果器的配置参数
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="key">参数名称</param>
    /// <returns>参数值</returns>
    /// <exception cref="InvalidCastException">类型错误时抛出</exception>
    public virtual T GetParameter<T>(string key)
    {
        object? value = GetParameter(key);
        if (value is T typedValue) return typedValue;
        throw new InvalidCastException($"参数 {key} 类型错误，预期：{typeof(T)}");
    }

    /// <summary>
    /// 设置音频源并返回当前实例
    /// </summary>
    /// <param name="source">音频源</param>
    /// <returns>当前效果器实例</returns>
    public IAudioEffect WithSource(ISampleProvider source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        return this;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    /// <returns>克隆的效果器实例</returns>
    public abstract IAudioEffect Clone();

    /// <summary>
    /// 返回当前效果器的调试信息
    /// </summary>
    public virtual string DebugInfo
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Enabled: {Enabled}");
            sb.AppendLine($"Priority: {Priority}");
            sb.AppendLine("Parameters:");
            foreach (var kvp in _parameters)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 当效果器参数发生变化时触发的事件
    /// </summary>
    public event EventHandler<ParameterChangedEventArgs>? ParameterChanged;

    /// <summary>
    /// 读取音频数据并应用效果
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public abstract int Read(float[] buffer, int offset, int count);

    /// <summary>
    /// 检查音频格式是否兼容
    /// </summary>
    /// <param name="waveFormat">目标音频格式</param>
    protected void ValidateWaveFormat(WaveFormat waveFormat)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        if (!waveFormat.Equals(Source.WaveFormat))
            throw new InvalidOperationException("音频格式不匹配");
    }

    /// <summary>
    /// 子类可以重写此方法以处理初始化完成后的逻辑
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// 子类可以重写此方法以处理启用状态变化后的逻辑
    /// </summary>
    protected virtual void OnEnabledChanged() { }

    /// <summary>
    /// 触发参数变化事件
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    protected virtual void OnParameterChanged(string key, object? value)
    {
        ParameterChanged?.Invoke(this, new ParameterChangedEventArgs(key, value));
    }
}