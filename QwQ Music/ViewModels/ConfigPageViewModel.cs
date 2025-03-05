namespace QwQ_Music.ViewModels;

using static Models.LanguageModel;

public partial class ConfigPageViewModel : ViewModelBase
{
    public static string LyricConfigName => Lang[nameof(LyricConfigName)];
}
