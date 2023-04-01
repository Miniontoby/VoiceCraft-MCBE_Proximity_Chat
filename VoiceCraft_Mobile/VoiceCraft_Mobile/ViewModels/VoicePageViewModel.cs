﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft_Mobile.Audio;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Repositories;
using VoiceCraft_Mobile.Views;
using Xamarin.Forms;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class VoicePageViewModel : BaseViewModel
    {
        [ObservableProperty]
        string statusMessage = "Connecting...";

        [ObservableProperty]
        public ObservableCollection<ParticipantModel> participants;


        public VoicePageViewModel()
        {
            App.Current.PageDisappearing += OnPageDisappearing;

            Network.Network.Current.signallingClient.OnDisconnect += OnDisconnect;
            Network.Network.Current.signallingClient.OnConnect += OnConnect;
            Network.Network.Current.signallingClient.OnBinded += OnBinded;
            Network.Network.Current.signallingClient.OnParticipantLogin += OnParticipantLogin;

            Network.Network.Current.voiceClient.OnConnect += VCConnected;
            Network.Network.Current.voiceClient.OnDisconnect += VCDisconnected;
        }

        [RelayCommand]
        async void Disconnect()
        {
            Network.Network.Current.Disconnect();
            await Utils.GoToPreviousPageAsync();
        }

        //Event Methods To Execute
        public void OnPageDisappearing(object sender, Page e)
        {
            if(e is VoicePage)
            {
                App.Current.PageDisappearing -= OnPageDisappearing;

                Network.Network.Current.signallingClient.OnDisconnect -= OnDisconnect;
                Network.Network.Current.signallingClient.OnConnect -= OnConnect;
                Network.Network.Current.signallingClient.OnBinded -= OnBinded;
                Network.Network.Current.signallingClient.OnParticipantLogin -= OnParticipantLogin;

                Network.Network.Current.voiceClient.OnConnect -= VCConnected;
                Network.Network.Current.voiceClient.OnDisconnect -= VCDisconnected;
            }
        }

        private void OnDisconnect(string reason)
        {
            if (reason != null)
                Utils.DisplayAlertAsync("Disconnect", reason);
            Utils.GoToPreviousPageAsync();
        }

        private void OnConnect(string key, string localServerId)
        {
            StatusMessage = $"Connecting Voice";
            var server = Database.GetServers().FirstOrDefault(x => x.LocalId == localServerId);
            server.Id = key;

            Database.EditServer(server);

            for (var i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].LocalId == server.LocalId)
                {
                    Servers[i] = server;
                    break;
                }
            }
            OnPropertyChanged(nameof(Servers));

            Network.Network.Current.voiceClient.Connect(Network.Network.Current.signallingClient.hostName, Network.Network.Current.signallingClient.VoicePort, key);
        }

        private void OnParticipantLogin(ParticipantModel participant)
        {
            participants.Add(participant);
            AudioPlayback.Current.AddMixerInput(participant.WaveProvider);
        }

        private void OnBinded(string name)
        {
            StatusMessage = $"Connected, Key: {Network.Network.Current.signallingClient.Key}\n Username: {name}";
        }

        private void VCDisconnected(string reason)
        {
            if (reason != null)
                Utils.DisplayAlertAsync("Disconnect", reason);

            Network.Network.Current.signallingClient.Disconnect();
            Utils.GoToPreviousPageAsync();
        }

        private void VCConnected()
        {
            StatusMessage = $"Connected, Key: {Network.Network.Current.signallingClient.Key}\nWaiting for binding...";
        }
    }
}
