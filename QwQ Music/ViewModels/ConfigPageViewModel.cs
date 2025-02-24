namespace QwQ_Music.ViewModels;
using static QwQ_Music.Models.LanguageModel;
public partial class ConfigPageViewModel : ViewModelBase {
    public static string LyricConfigName => Lang[nameof(LyricConfigName)];
}