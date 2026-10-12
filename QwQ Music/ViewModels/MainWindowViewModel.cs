using QwQ_Music.Common.Managers;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    public static DrawerManager DrawerManager => DrawerManager.Instance;
}