using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public class NavigationViewModel : ViewModelBase
{
    private string NavViewName { get; set; }

    protected NavigationViewModel(string navViewName)
    {
        NavViewName = navViewName;
        NavigateService.NavigateEvents[NavViewName] = NavigateEvent;
    }

    private void NavigateEvent(int index)
    {
        SelectedIndex = index;
    }

    public int SelectedIndex
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            NavigateService.NavigateTo(NavViewName, field);
            OnNavigateTo(field);
        }
    }

    protected virtual void OnNavigateTo(int index) { }
}
