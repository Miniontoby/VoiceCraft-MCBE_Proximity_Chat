﻿using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;
using VoiceCraft.Core.Settings;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title { get => "Settings"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private bool _audioSettingsExpanded = false;

        [ObservableProperty]
        private bool _generalSettingsExpanded = false;

        [ObservableProperty]
        private AudioSettings _audioSettings;

        [ObservableProperty]
        private ThemeSettings _themeSettings;

        [ObservableProperty]
        private ServersSettings _serversSettings;

        [ObservableProperty]
        private ObservableCollection<string> _themeKeys;

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private float _microphoneValue;

        public SettingsViewModel(SettingsService settings, ThemesService themes)
        {
            _audioSettings = settings.Get<AudioSettings>(App.SettingsId);
            _themeSettings = settings.Get<ThemeSettings>(App.SettingsId);
            _serversSettings = settings.Get<ServersSettings>(App.SettingsId);

            _themeKeys = new ObservableCollection<string>(themes.ThemeKeys);

            _themeSettings.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == nameof(ThemeSettings.SelectedTheme) && Application.Current != null)
                    Application.Current.RequestedThemeVariant = new Avalonia.Styling.ThemeVariant(ThemeSettings.SelectedTheme, null);
            };
        }
    }
}