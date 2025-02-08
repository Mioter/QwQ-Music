using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using QwQ_Music.Models;
using QwQ_Music.Tools;

namespace QwQ_Music.Common;

public static class ConfigService
{
    private static readonly string DirectoryPath;
    private static readonly JsonConfig<ObservableCollection<MusicItemModel>?> MusicInfoConfig;
    private static readonly JsonConfig<MusicListModel?> MusicListConfig;

    // 静态构造函数初始化路径和配置
    static ConfigService()
    {
        string workDirectory = Directory.GetCurrentDirectory();
        DirectoryPath = Path.Combine(workDirectory, "MusicCache");

        // 初始化配置文件
        MusicInfoConfig = new JsonConfig<ObservableCollection<MusicItemModel>?>(Path.Combine(DirectoryPath, "music_info.json"));
        MusicListConfig = new JsonConfig<MusicListModel?>(Path.Combine(DirectoryPath, "music_list.json"));
    }

    // 确保目录和文件存在
    private async static Task EnsureDirectoryAndFilesExistAsync()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
        }

        // 确保文件存在
        await EnsureFileExistsAsync(MusicInfoConfig.FilePath);
        await EnsureFileExistsAsync(MusicListConfig.FilePath);
    }

    // 确保单个文件存在
    private async static Task EnsureFileExistsAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            await using (File.Create(filePath)) { }
        }
    }

    // 保存音乐项信息到配置文件
    public async static Task SaveMusicInfoAsync(ObservableCollection<MusicItemModel>? musicItems)
    {
        await EnsureDirectoryAndFilesExistAsync();
        await MusicInfoConfig.SaveToJsonAsync(musicItems);
    }

    // 保存音乐项列表到配置文件
    public async static Task SaveMusicListAsync(MusicListModel musicList)
    {
        await EnsureDirectoryAndFilesExistAsync();
        await MusicListConfig.SaveToJsonAsync(musicList);
    }

    // 从配置文件中获取当前音乐列表
    public async static Task<ObservableCollection<MusicItemModel>?> GetMusicInfoAsync()
    {
        await EnsureDirectoryAndFilesExistAsync();
        return await MusicInfoConfig.LoadFromJsonAsync();
    }

    // 从配置文件中获取音乐项信息
    public async static Task<MusicListModel?> GetMusicListAsync()
    {
        await EnsureDirectoryAndFilesExistAsync();
        return await MusicListConfig.LoadFromJsonAsync();
    }
}
