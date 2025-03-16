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

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (!SetProperty(ref _selectedIndex, value))
                return;

            NavigateService.NavigateTo(NavViewName, _selectedIndex);
            OnNavigateTo(_selectedIndex);
        }
    }

    protected virtual void OnNavigateTo(int index) { }
}
