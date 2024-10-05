﻿using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _isRecording;
        private readonly AndroidAudioRecorder _nativeRecorder = new AndroidAudioRecorder();

        public bool IsRecording => _isRecording;
        public WaveFormat WaveFormat { get => _nativeRecorder.WaveFormat; set => _nativeRecorder.WaveFormat = value; }
        public int BufferMilliseconds { get => _nativeRecorder.BufferMilliseconds; set => _nativeRecorder.BufferMilliseconds = value; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder()
        {
            _nativeRecorder.DataAvailable += InvokeDataAvailable;
            _nativeRecorder.RecordingStopped += InvokeRecordingStopped;
        }

        public void SetDevice(string device)
        {
            
        }

        public void StartRecording()
        {
            _nativeRecorder.StartRecording();
            _isRecording = true;
        }

        public void StopRecording()
        {
            _nativeRecorder.StopRecording();
            _isRecording = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void InvokeDataAvailable(object? sender, WaveInEventArgs e)
        {
            DataAvailable?.Invoke(sender, e);
        }

        private void InvokeRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _isRecording = false;
            RecordingStopped?.Invoke(sender, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nativeRecorder.Dispose();
            }
        }
    }
}
