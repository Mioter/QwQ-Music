namespace QwQ_Music.ViewModels;

using static Models.LanguageModel;

public class ConfigPageViewModel() : NavigationViewModel("设置")
{
    public static string LyricConfigName => Lang[nameof(LyricConfigName)];
}
