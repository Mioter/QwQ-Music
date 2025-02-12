using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QwQ_Music.Models;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services;

public class ConfigService
{
    private enum FileClass
    {
        MusicCache,
        ConfigInfo,
    }
    
    private readonly JsonConfig<ObservableCollection<MusicItemModel>?> _musicInfoConfig;
    private readonly JsonConfig<MusicListModel?> _musicListConfig;
    private readonly JsonConfig<ConfigInfoModel?> _configInfoConfig;
        
    private readonly Dictionary<FileClass, string> _directoryPath;

    // 静态构造函数初始化路径和配置
    public ConfigService()
    {
        string workDirectory = Directory.GetCurrentDirectory();

        _directoryPath = new Dictionary<FileClass, string>
        {
            {
                FileClass.MusicCache, Path.Combine(workDirectory, "MusicCache")
            },
            {
                FileClass.ConfigInfo, Path.Combine(workDirectory, "ConfigInfo")
            },
        };

        // 初始化配置文件
        _musicInfoConfig = new JsonConfig<ObservableCollection<MusicItemModel>?>(Path.Combine(_directoryPath[FileClass.MusicCache], "music_info.json"));
        _musicListConfig = new JsonConfig<MusicListModel?>(Path.Combine(_directoryPath[FileClass.MusicCache], "music_list.json"));
        _configInfoConfig = new JsonConfig<ConfigInfoModel?>(Path.Combine(_directoryPath[FileClass.ConfigInfo], "config_info.json"));
        
    }

    // 确保目录和文件存在
    private async Task EnsureDirectoryAndFilesExistAsync()
    {
        foreach (var directory in _directoryPath.Where(directory => !Directory.Exists(directory.Value)))
            Directory.CreateDirectory(directory.Value);

        // 确保文件存在
        await EnsureFileExistsAsync(_musicInfoConfig.FilePath);
        await EnsureFileExistsAsync(_musicListConfig.FilePath);
    }

    // 确保单个文件存在
    private async static Task EnsureFileExistsAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            await using (File.Create(filePath)) { }
        }
    }

    // 保存音乐信息到配置文件
    public async Task SaveMusicInfoAsync(ObservableCollection<MusicItemModel>? musicItems)
    {
        await EnsureDirectoryAndFilesExistAsync();
        await _musicInfoConfig.SaveToJsonAsync(musicItems);
    }

    // 从配置文件中获取当前音乐信息
    public async Task<ObservableCollection<MusicItemModel>?> GetMusicInfoAsync()
    {
        await EnsureDirectoryAndFilesExistAsync();
        return await _musicInfoConfig.LoadFromJsonAsync();
    }

    // 保存音乐列表到配置文件
    public async Task SaveMusicListAsync(MusicListModel musicList)
    {
        await EnsureDirectoryAndFilesExistAsync();
        await _musicListConfig.SaveToJsonAsync(musicList);
    }

    // 从配置文件中获取音乐列表
    public async Task<MusicListModel?> GetMusicListAsync()
    {
        await EnsureDirectoryAndFilesExistAsync();
        return await _musicListConfig.LoadFromJsonAsync();
    }
    
    // 保存配置信息到配置文件
    public async Task SaveConfigInfoAsync(ConfigInfoModel configInfo)
    {
        await EnsureDirectoryAndFilesExistAsync();
        await _configInfoConfig.SaveToJsonAsync(configInfo);
    }

    // 从配置文件中获取配置信息
    public async Task<ConfigInfoModel?> GetConfigInfoAsync()
    {
        await EnsureDirectoryAndFilesExistAsync();
        return await _configInfoConfig.LoadFromJsonAsync();
    }
}
