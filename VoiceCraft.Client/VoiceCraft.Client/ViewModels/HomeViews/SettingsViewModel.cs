﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
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
        private ObservableCollection<string> _themes;

        [ObservableProperty]
        private AudioSettings _audioSettings;

        [ObservableProperty]
        private ThemeSettings _themeSettings;

        [ObservableProperty]
        private ServersSettings _serversSettings;

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private float _microphoneValue;

        public SettingsViewModel(SettingsService settings, ThemesService themes)
        {
            _themes = new ObservableCollection<string>(themes.ThemeNames);

            _audioSettings = settings.Get<AudioSettings>(App.SettingsId);
            _themeSettings = settings.Get<ThemeSettings>(App.SettingsId);
            _serversSettings = settings.Get<ServersSettings>(App.SettingsId);

            _themeSettings.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == nameof(ThemeSettings.SelectedTheme))
                {
                    themes.SwitchTheme(ThemeSettings.SelectedTheme);
                }
            };
        }
    }

    internal class ObservableCollection : ObservableCollection<string>
    {
        public ObservableCollection(IEnumerable<string> collection) : base(collection)
        {
        }
    }
}