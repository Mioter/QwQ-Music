using QwQ_Music.Common.Services;

namespace QwQ_Music.ViewModels.Bases;

public class NavigationViewModel : ViewModelBase
{
    protected NavigationViewModel(string navViewName)
    {
        NavViewName = navViewName;
        NavigateService.NavigateToEvents[NavViewName] = NavigateEvent;
    }

    protected string NavViewName { get; }

    public int NavigationIndex
    {
        get;
        set
        {
            if (!InNavigateTo(value))
                return;

            if (!SetProperty(ref field, value))
                return;

            NavigateService.NavigateEvent(NavViewName, field);
            OnNavigateTo(field);
        }
    }

    private void NavigateEvent(int index)
    {
        NavigationIndex = index;
    }

    protected virtual bool InNavigateTo(int index)
    {
        return true;
    }

    protected virtual void OnNavigateTo(int index) { }
}
