﻿using Android.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.PDK.Audio;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAudioService : AudioService
    {
        //This may or may not include bugged devices that can crash the application.
        protected static AudioDeviceType[] _allowedDeviceTypes = [
            AudioDeviceType.AuxLine,
            AudioDeviceType.BluetoothA2dp,
            AudioDeviceType.BluetoothSco,
            AudioDeviceType.BuiltinMic,
            AudioDeviceType.BuiltinEarpiece,
            AudioDeviceType.BuiltinSpeaker,
            AudioDeviceType.Dock,
            AudioDeviceType.Hdmi,
            AudioDeviceType.HdmiArc,
            AudioDeviceType.LineAnalog,
            AudioDeviceType.LineDigital,
            AudioDeviceType.UsbAccessory,
            AudioDeviceType.UsbDevice,
            AudioDeviceType.WiredHeadphones,
            AudioDeviceType.WiredHeadset
            ];
        private AudioManager _audioManager;

        public NativeAudioService(AudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public override IAudioRecorder CreateAudioRecorder()
        {
            return new AudioRecorder(_audioManager);
        }

        public override IAudioPlayer CreateAudioPlayer()
        {
            return new AudioPlayer(_audioManager);
        }

        public override string GetDefaultInputDevice()
        {
            return "Default";
        }

        public override string GetDefaultOutputDevice()
        {
            return "Default";
        }

        public override string GetDefaultPreprocessor()
        {
            return "None";
        }

        public override string GetDefaultEchoCanceler()
        {
            return "None";
        }

        public override List<string> GetInputDevices()
        {
            var devices = new List<string>() { GetDefaultInputDevice() };

            var audioDevices = _audioManager.GetDevices(GetDevicesTargets.Inputs)?.Where(x => _allowedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            if (audioDevices == null) return devices;

            foreach (var audioDevice in audioDevices)
            {
                var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }
            return devices;
        }

        public override List<string> GetOutputDevices()
        {
            var devices = new List<string>() { GetDefaultOutputDevice() };

            var audioDevices = _audioManager.GetDevices(GetDevicesTargets.Outputs)?.Where(x => _allowedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            if (audioDevices == null) return devices;

            foreach (var audioDevice in audioDevices)
            {
                var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }
            return devices;
        }

        public override List<string> GetPreprocessors()
        {
            var preprocessors = new List<string>() { GetDefaultPreprocessor() };
            preprocessors.AddRange(_registeredPreprocessors.Keys);
            return preprocessors;
        }

        public override List<string> GetEchoCancelers()
        {
            var echoCancelers = new List<string>() { GetDefaultEchoCanceler() };
            echoCancelers.AddRange(_registeredEchoCancelers.Keys);
            return echoCancelers;
        }
    }
}
