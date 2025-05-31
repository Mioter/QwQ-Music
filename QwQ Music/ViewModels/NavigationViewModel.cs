using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public class NavigationViewModel : ViewModelBase
{
    private string NavViewName { get; }

    protected NavigationViewModel(string navViewName)
    {
        NavViewName = navViewName;
        NavigateService.NavigateEvents[NavViewName] = NavigateEvent;
    }

    private void NavigateEvent(int index)
    {
        NavigationIndex = index;
    }

    public int NavigationIndex
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            NavigateService.NavigateEvent(NavViewName, field);
            OnNavigateTo(field);
        }
    }

    protected virtual void OnNavigateTo(int index) { }
}
