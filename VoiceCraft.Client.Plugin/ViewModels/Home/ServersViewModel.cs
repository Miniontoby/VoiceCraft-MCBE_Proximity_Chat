﻿using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.Views;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title => "Servers";

        private NavigationService _navigator;
        private INotificationMessageManager _manager;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server? _selectedServer;

        partial void OnSelectedServerChanged(Server? value)
        {
            if (value == null) return;
            var model = _navigator.NavigateTo<ServerView>();
            model.SelectedServer = value;
            SelectedServer = null;
            // OnPropertyChanged(nameof(SelectedServer));
            // TODO this still needs to be fixed, since it DOESN'T actually unset the selected server
        }

        public ServersViewModel(NavigationService navigator, NotificationMessageManager manager, SettingsService settings)
        {
            _navigator = navigator;
            _manager = manager;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(Plugin.PluginId);
        }

        [RelayCommand]
        public async Task DeleteServer(Server server)
        {
            Servers.RemoveServer(server);
            _manager.CreateMessage()
                .Accent("#1751C3")
                .Animates(true)
                .Background("#333")
                .HasBadge("Info")
                .HasMessage($"{server.Name} has been removed.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                .Queue();
            await _settings.SaveAsync();
        }

        [RelayCommand]
        public void EditServer(Server? server)
        {
            if (server == null) return; //Somehow can be null.
            var model = _navigator.NavigateTo<EditServerView>();
            model.Server = server;
        }
    }
}