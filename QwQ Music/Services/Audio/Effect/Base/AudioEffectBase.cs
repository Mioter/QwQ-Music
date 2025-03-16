using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QwQ_Music.Services.Audio.Effect.Base;

/// <summary>
/// 音频效果器基类，提供基础实现
/// </summary>
public abstract class AudioEffectBase : IAudioEffect
{
    private readonly Dictionary<string, object?> _parameters = new();
    private bool _enabled;

    /// <summary>
    /// 当前音频源提供者
    /// </summary>
    protected ISampleProvider Source { get; private set; } = null!;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;
            _enabled = value;
            OnEnabledChanged();
        }
    }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public WaveFormat WaveFormat => Source.WaveFormat;

    /// <inheritdoc />
    public void Initialize(ISampleProvider source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        OnInitialized();
    }

    /// <inheritdoc />
    public virtual Task InitializeAsync(ISampleProvider source)
    {
        Initialize(source);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void SetParameter<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            LoggerService.Error($"参数名称不能为空: {nameof(key)}");
        _parameters[key] = value;
        OnParameterChanged(key, value);
    }

    /// <inheritdoc />
    public virtual T GetParameter<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            LoggerService.Error($"参数名称不能为空{nameof(key)}");
        return (T)(
            _parameters.TryGetValue(key, out object? value)
                ? value!
                : throw new KeyNotFoundException($"未找到参数：{key}")
        );
    }

    /// <inheritdoc />
    public IAudioEffect WithSource(ISampleProvider source)
    {
        Source = source;
        return this;
    }

    /// <inheritdoc />
    public abstract IAudioEffect Clone();

    /// <inheritdoc />
    public virtual string DebugInfo =>
        $"Name: {Name}\nEnabled: {Enabled}\nPriority: {Priority}\nParameters: {FormatParameters()}";

    /// <inheritdoc />
    public event EventHandler<ParameterChangedEventArgs>? ParameterChanged;

    /// <inheritdoc />
    public abstract int Read(float[] buffer, int offset, int count);

    /// <summary>
    /// 初始化完成时的回调方法
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// 启用状态变更时的回调方法
    /// </summary>
    protected virtual void OnEnabledChanged() { }

    /// <summary>
    /// 触发参数变更事件
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    protected void OnParameterChanged(string key, object? value) =>
        ParameterChanged?.Invoke(this, new ParameterChangedEventArgs(key, value));

    /// <summary>
    /// 格式化参数字典为可读字符串
    /// </summary>
    /// <returns>格式化后的参数信息</returns>
    private string FormatParameters()
    {
        var sb = new StringBuilder();
        foreach ((string k, object? v) in _parameters)
            sb.AppendLine($"  {k}: {v}");
        return sb.ToString();
    }
}
