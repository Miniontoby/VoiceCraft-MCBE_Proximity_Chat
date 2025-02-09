using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiteNetLib;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Processes
{
    public class VoipBackgroundProcess : IBackgroundProcess
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private bool _muted;
        private bool _deafened;
        
        //Events
        public event Action<string>? OnUpdateTitle;
        public event Action<string>? OnUpdateDescription;
        public event Action<bool>? OnUpdateMute;
        public event Action<bool>? OnUpdateDeafen;
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        
        //Public Variables
        public CancellationTokenSource TokenSource { get; }
        public ConnectionStatus ConnectionStatus => _voiceCraftClient.ConnectionStatus;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnUpdateTitle?.Invoke(value);
            }
        }
        
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnUpdateDescription?.Invoke(value);
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                OnUpdateMute?.Invoke(value);
            }
        }
        
        public bool Deafened
        {
            get => _deafened;
            set
            {
                _deafened = value;
                OnUpdateDeafen?.Invoke(value);
            }
        }

        //Privates
        private readonly VoiceCraftClient _voiceCraftClient;
        private readonly NotificationService _notificationService;
        private readonly AudioService _audioService;
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;
        private readonly string _ip;
        private readonly int _port;

        public VoipBackgroundProcess(string ip, int port, NotificationService notificationService, AudioService audioService)
        {
            TokenSource = new CancellationTokenSource();
            _voiceCraftClient = new VoiceCraftClient();
            _notificationService = notificationService;
            _audioService = audioService;
            _voiceCraftClient.OnConnected += ClientOnConnected;
            _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
            _ip = ip;
            _port = port;
        }

        public void Start()
        {
            _voiceCraftClient.Connect(_ip, _port, LoginType.Login);
            Title = "VoiceCraft Client is running";
            Description = "Status: Initializing...";

            _audioRecorder = _audioService.CreateAudioRecorder();
            _audioRecorder.WaveFormat = VoiceCraftClient.WaveFormat;
            _audioRecorder.StartRecording();
            
            Title = "VoiceCraft Client is running";
            Description = "Status: Connecting...";

            while (_voiceCraftClient.ConnectionStatus != ConnectionStatus.Disconnected)
            {
                if (TokenSource.Token.IsCancellationRequested)
                    _voiceCraftClient.Disconnect();
                _voiceCraftClient.Update(); //Update all networking processes.
                Task.Delay(1).GetAwaiter().GetResult();
            }
        }
        
        public void ToggleMute()
        {
            Muted = !Muted;
        }

        public void ToggleDeafen()
        {
            Deafened = !Deafened;
        }

        public void Disconnect()
        {
            TokenSource.Cancel();
        }
        
        public void Dispose()
        {
            _voiceCraftClient.Dispose();
            if (_audioRecorder?.CaptureState != CaptureState.Stopped)
                _audioRecorder?.StopRecording();
            _audioRecorder?.Dispose();
            _audioRecorder = null;

            if (_audioPlayer?.PlaybackState != PlaybackState.Stopped)
                _audioPlayer?.Stop();
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            GC.SuppressFinalize(this);
        }
        
        private void ClientOnConnected()
        {
            Title = "VoiceCraft Client is running";
            Description = "Status: Connected!";
            Dispatcher.UIThread.Invoke(() => OnConnected?.Invoke());
        }
        
        private void ClientOnDisconnected(DisconnectInfo obj)
        {
            Title = "VoiceCraft Client is running"; 
            Description = $"Status: Disconnected! Reason: {obj.Reason}";
            Dispatcher.UIThread.Invoke(() =>
            {
                _notificationService.SendNotification($"Disconnected! Reason: {obj.Reason}");
                OnDisconnected?.Invoke(obj);
            });
        }
    }
}