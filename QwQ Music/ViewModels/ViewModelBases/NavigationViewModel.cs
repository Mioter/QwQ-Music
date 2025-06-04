using QwQ_Music.Services;

namespace QwQ_Music.ViewModels.ViewModelBases;

public class NavigationViewModel : ViewModelBase
{
    protected string NavViewName { get; }

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
            if (!InNavigateTo(value))
                return;

            if (!SetProperty(ref field, value))
                return;

            NavigateService.NavigateEvent(NavViewName, field);
            OnNavigateTo(field);
        }
    }

    protected virtual bool InNavigateTo(int index)
    {
        return true;
    }

    protected virtual void OnNavigateTo(int index) { }
}
