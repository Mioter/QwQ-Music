using QwQ_Music.Models;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

using static LanguageModel;

public class ConfigPageViewModel() : NavigationViewModel("设置")
{
    public static string LyricConfigName => Lang[nameof(LyricConfigName)];
}
