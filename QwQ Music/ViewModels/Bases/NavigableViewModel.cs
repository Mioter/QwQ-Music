using QwQ_Music.Common.Services;

namespace QwQ_Music.ViewModels.Bases;

public class NavigableViewModel : ViewModelBase {
    protected NavigableViewModel(string viewModelName) {
        ViewModelName = viewModelName;
        NavigateService.NavigateToEvents[ViewModelName] = NavigateEvent;
    }

    protected string ViewModelName { get; }

    public int NavigationIndex {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;

            NavigateService.NavigateEvent(ViewModelName, field);
            OnNavigateTo(field);
        }
    }

    private void NavigateEvent(int index) { NavigationIndex = index; }

    protected virtual void OnNavigateTo(int index) { }
}