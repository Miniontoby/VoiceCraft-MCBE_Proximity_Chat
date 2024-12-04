using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class AudioSettings : Setting<AudioSettings>
    {
        public override event Action<AudioSettings>? OnUpdated;

        public string InputDevice
        {
            get => _inputDevice;
            set
            {
                _inputDevice = value;
                OnUpdated?.Invoke(this);
            }
        }

        public string OutputDevice
        {
            get => _outputDevice;
            set
            {
                _outputDevice = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Guid Preprocessor
        {
            get => _preprocessor;
            set
            {
                _preprocessor = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Guid EchoCanceler
        {
            get => _echoCanceler;
            set
            {
                _echoCanceler = value;
                OnUpdated?.Invoke(this);
            }
        }

        public float MicrophoneSensitivity
        {
            get => _microphoneSensitivity;
            set
            {
                if(value is > 1 or < 0)
                    throw new ArgumentException("Microphone sensitivity must be between 0 and 1.");
                _microphoneSensitivity = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool Aec
        {
            get => _aec;
            set
            {
                _aec = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool Agc
        {
            get => _agc;
            set
            {
                _agc = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool Denoiser
        {
            get => _denoiser;
            set
            {
                _denoiser = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool Vad
        {
            get => _vad;
            set
            {
                _vad = value;
                OnUpdated?.Invoke(this);
            }
        }

        private string _inputDevice = "Default";
        private string _outputDevice = "Default";
        private Guid _preprocessor = Guid.Empty;
        private Guid _echoCanceler = Guid.Empty;
        private float _microphoneSensitivity = 0.04f;
        private bool _aec;
        private bool _agc;
        private bool _denoiser;
        private bool _vad;

        public override object Clone()
        {
            var clone = (AudioSettings)MemberwiseClone();
            clone.OnUpdated = null;
            return clone;
        }
    }
}