﻿using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class LyricConfigPage : Grid
{
    public LyricConfigPage()
    {
        InitializeComponent();
        DataContext = new LyricConfigPageViewModel();
    }
}
