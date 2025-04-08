using QwQ_Music.Models;

namespace QwQ_Music.ViewModels;

using static LanguageModel;

public class ConfigPageViewModel() : NavigationViewModel("设置")
{
    public static string LyricConfigName => Lang[nameof(LyricConfigName)];
}
