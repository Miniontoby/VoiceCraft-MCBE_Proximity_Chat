using Android.Media.Audiofx;
using System;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeEchoCanceler : IEchoCanceler
    {
        public bool IsNative => true;

        private AcousticEchoCanceler? _echoCanceler;
        private AudioRecorder? _recorder;
        private bool _initialized;
        private bool _disposed;

        ~NativeEchoCanceler()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder, IAudioPlayer player) //We don't need to have the audio player, but it's there for other compatibility reasons.
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new ArgumentException(Localizer.Get("Android.NativeAEC.Exception.AndroidRecorder"), nameof(recorder));
            if (_echoCanceler != null)
            {
                _echoCanceler.Release();
                _echoCanceler.Dispose();
                _echoCanceler = null;
            }

            _recorder = audioRecorder;
            _initialized = false;
        }

        public void EchoPlayback(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_initialized) return;
            if (_recorder?.SessionId == null) throw new InvalidOperationException(Localizer.Get("Android.NativeAEC.Exception.Init"));

            if (_echoCanceler != null)
            {
                _echoCanceler.Release();
                _echoCanceler.Dispose();
                _echoCanceler = null;
            }
            
            _echoCanceler = AcousticEchoCanceler.Create((int)_recorder.SessionId);
            _initialized = true;
        }

        public void EchoPlayback(byte[] buffer) => EchoPlayback(buffer.AsSpan());

        public void EchoCancel(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_initialized) return;
            if (_recorder?.SessionId == null) throw new InvalidOperationException(Localizer.Get("Android.NativeAEC.Exception.Init"));
            if (_echoCanceler != null)
            {
                _echoCanceler.Release();
                _echoCanceler.Dispose();
                _echoCanceler = null;
            }
            
            _echoCanceler = AcousticEchoCanceler.Create((int)_recorder.SessionId);
            _initialized = true;
        }

        public void EchoCancel(byte[] buffer) => EchoCancel(buffer.AsSpan());

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(NativeEchoCanceler));
        }
    }
}