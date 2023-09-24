﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        ParticipantDisplayModel selectedParticipant = new ParticipantDisplayModel();

        [ObservableProperty]
        bool showSlider = false;

        [ObservableProperty]
        ObservableCollection<ParticipantDisplayModel> participants = new ObservableCollection<ParticipantDisplayModel>();

        [RelayCommand]
        public void MuteUnmute()
        {
            IsMuted = !IsMuted;
            var message = new MuteUnmuteMessage() { Value = IsMuted };
            MessagingCenter.Send(message, "MuteUnmute");
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            IsDeafened = !IsDeafened;
            var message = new DeafenUndeafen() { Value = IsDeafened };
            MessagingCenter.Send(message, "DeafenUndeafen");
        }

        [RelayCommand]
        public void Disconnect()
        {
            MessagingCenter.Send(new DisconnectMessage(), "Disconnect");
        }

        //Page codebehind to viewmodel.
        [RelayCommand]
        public void OnAppearing()
        {
            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.Navigation.PopAsync();
                });
                return;
            }

            MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message =>
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.Navigation.PopAsync();
                });
            });

            MessagingCenter.Subscribe<UpdateMessage>(this, "Update", message =>
            {
                for (int i = 0; i < message.Participants.Count; i++)
                {
                    var participant = message.Participants[i];
                    var displayParticipant = Participants.FirstOrDefault(x => x.Key == participant.Key);
                    if (displayParticipant != null)
                    {
                        displayParticipant.IsDeafened = participant.IsDeafened;
                        displayParticipant.IsMuted = participant.IsMuted;
                        displayParticipant.IsSpeaking = participant.IsSpeaking;
                    }
                    else
                    {
                        Participants.Add(participant);
                    }
                }

                for (int i = 0; i < Participants.Count; i++)
                {
                    var participant = message.Participants.FirstOrDefault(x => x.Key == Participants[i].Key);
                    if (participant == null)
                    {
                        Participants.Remove(Participants[i]);
                    }
                }

                IsSpeaking = message.IsSpeaking;
                IsDeafened = message.IsDeafened;
                IsMuted = message.IsMuted;
                StatusText = message.StatusMessage;
            });

            MessagingCenter.Subscribe<UpdateStatusMessage>(this, "UpdateStatus", message =>
            {
                StatusText = message.StatusMessage;
            });

            MessagingCenter.Subscribe<DisconnectMessage>(this, "Disconnected", message =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(message.Reason))
                        Shell.Current.DisplayAlert("Disconnected!", message.Reason, "OK");
                });
            });
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<StopServiceMessage>(this, "ServiceStopped");
            MessagingCenter.Unsubscribe<UpdateMessage>(this, "Update");
            MessagingCenter.Unsubscribe<UpdateStatusMessage>(this, "UpdateStatus");
            MessagingCenter.Unsubscribe<DisconnectMessage>(this, "Disconnected");
        }

        [RelayCommand]
        public void ShowParticipantVolume(ushort key)
        {
            var participant = Participants.FirstOrDefault(x => x.Key == key);
            if (participant != null)
            {
                SelectedParticipant = participant;
                ShowSlider = true;
            }
        }

        [RelayCommand]
        public void HideParticipantVolume()
        {
            ShowSlider = false;
        }
    }
}
