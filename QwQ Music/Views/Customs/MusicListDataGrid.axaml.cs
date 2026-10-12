using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.Views.Customs;

[TemplatePart(IsRequired = true, Name = "PART_Data", Type = typeof(DataGrid))]
public partial class MusicListDataGrid : TemplatedControl {
    public static readonly DirectProperty<MusicListDataGrid, bool> IsCustomizableProperty =
        AvaloniaProperty.RegisterDirect<MusicListDataGrid, bool>(
            nameof(IsCustomizable),
            o => o.IsCustomizable,
            (o, v) => o.IsCustomizable = v,
            true);

    private DataGrid? _data;

    public bool IsCustomizable {
        get;
        set => SetAndRaise(IsCustomizableProperty, ref field, value);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_data is null)
            return;
        if (DataContext is not MusicItemsViewModelBase context)
            throw new InvalidOperationException();
        Debug.Assert(_data.SelectedItems.Count == 0 || _data.SelectedItems[0] is MusicItemModel);
        context.SelectedItems = _data.SelectedItems.Cast<MusicItemModel>().ToList();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);
        _data = e.NameScope.Find<DataGrid>("PART_Data");
        _data?.SelectionChanged += OnSelectionChanged;
    }

    protected override void OnUnloaded(RoutedEventArgs e) {
        _data?.SelectionChanged -= OnSelectionChanged;
        base.OnUnloaded(e);
    }

    [RelayCommand]
    public void ScrollToCurrent() { _data?.ScrollIntoView(_data.SelectedItem, _data.CurrentColumn); }
}