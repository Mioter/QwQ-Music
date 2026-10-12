namespace QwQ_Music.ViewModels.Dialogs;

public class ViewTextViewModel(string text, string title = "标题未设置") {
    public string Title { get; } = title;

    public string Text { get; } = text;
}