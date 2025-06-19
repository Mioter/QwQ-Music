using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace QwQ_Music.Services.ConfigIO;

/// <summary>
/// 简洁高效的INI配置服务，支持Section和Key-Value，线程安全，跨平台。
/// </summary>
public class IniConfigService
{
    /// <summary>
    /// 存储所有节及其对应的键值对。
    /// 外层Key为节名（Section），内层Key为键名，Value为对应的值。
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, string>> _data = [];

    /// <summary>
    /// 读写锁，保证多线程环境下的数据安全。
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// 当前加载或保存的文件路径。
    /// </summary>
    private string? _filePath;

    /// <summary>
    /// 默认构造函数。
    /// </summary>
    public IniConfigService() { }

    /// <summary>
    /// 通过文件路径构造并自动加载配置。
    /// </summary>
    /// <param name="filePath">INI文件路径</param>
    public IniConfigService(string? filePath)
    {
        Load(filePath);
    }

    /// <summary>
    /// 加载指定路径的INI文件。
    /// </summary>
    /// <param name="filePath">INI文件路径</param>
    public void Load(string? filePath)
    {
        _lock.EnterWriteLock();
        try
        {
            _data.Clear();
            _filePath = filePath;

            if (!File.Exists(filePath))
                return;

            string section = string.Empty;

            foreach (string line in File.ReadAllLines(filePath, Encoding.UTF8))
            {
                string trimmed = line.Trim();

                // 跳过空行和注释行
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                    continue;

                // 处理节（Section）
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    section = trimmed[1..^1].Trim();
                    if (!_data.TryGetValue(section, out _))
                    {
                        _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    continue;
                }

                // 处理键值对
                int idx = trimmed.IndexOf('=');
                if (idx <= 0)
                    continue;

                string key = trimmed[..idx].Trim();
                string value = trimmed[(idx + 1)..].Trim();
                if (!_data.TryGetValue(section, out var dict))
                {
                    dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _data[section] = dict;
                }
                dict[key] = value;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 保存配置到文件。
    /// </summary>
    /// <param name="filePath">要保存的文件路径（可选，默认保存到上次加载的路径）</param>
    public void Save(string? filePath = null)
    {
        _lock.EnterReadLock();
        try
        {
            string? path = filePath ?? _filePath;
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("未指定文件路径");

            var sb = new StringBuilder();

            foreach (var section in _data)
            {
                // 写入节名
                if (!string.IsNullOrEmpty(section.Key))
                    sb.AppendLine($"[{section.Key}]");

                // 写入该节下所有键值对
                foreach (var kv in section.Value)
                    sb.AppendLine($"{kv.Key}={kv.Value}");

                sb.AppendLine(); // 节之间空一行
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定节和键的值。
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="section">节名（可选，默认全局节）</param>
    /// <returns>对应的值，若不存在则返回null</returns>
    public string? Get(string key, string? section = "")
    {
        _lock.EnterReadLock();
        try
        {
            section ??= string.Empty;
            if (_data.TryGetValue(section, out var dict) && dict.TryGetValue(key, out string? value))
                return value;
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 设置指定节和键的值。
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">要设置的值</param>
    /// <param name="section">节名（可选，默认全局节）</param>
    public void Set(string key, string value, string? section = "")
    {
        _lock.EnterWriteLock();
        try
        {
            section ??= string.Empty;
            if (!_data.TryGetValue(section, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _data[section] = dict;
            }
            dict[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 判断指定节下是否包含某个键。
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="section">节名（可选，默认全局节）</param>
    /// <returns>存在返回true，否则false</returns>
    public bool ContainsKey(string key, string? section = "")
    {
        _lock.EnterReadLock();
        try
        {
            section ??= string.Empty;
            return _data.TryGetValue(section, out var dict) && dict.ContainsKey(key);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 移除指定节下的某个键。
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="section">节名（可选，默认全局节）</param>
    public void Remove(string key, string? section = "")
    {
        _lock.EnterWriteLock();
        try
        {
            section ??= string.Empty;
            if (_data.TryGetValue(section, out var dict))
            {
                dict.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 移除整个节及其所有键值对。
    /// </summary>
    /// <param name="section">节名（可选，默认全局节）</param>
    public void RemoveSection(string? section)
    {
        _lock.EnterWriteLock();
        try
        {
            section ??= string.Empty;
            _data.Remove(section);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
