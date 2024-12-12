using System;
using NAudio.Wave;
using SpeexDSPSharp.Core;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Audio
{
    public class SpeexDspEchoCanceler : IEchoCanceler
    {
        private const int FilterLengthMs = 100;

        public bool IsNative => false;

        public bool IsAvailable => true;

        public bool Enabled { get; set; }
        
        private bool _disposed;
        private WaveFormat? _waveFormat;
        private int _bytesPerFrame;
        private SpeexDSPEchoCanceler? _echoCanceler;

        public void Init(IAudioRecorder recorder, IAudioPlayer player)
        {
            ThrowIfDisposed();

            if (_echoCanceler != null)
            {
                _echoCanceler.Dispose();
                _echoCanceler = null;
            }
            
            _waveFormat = recorder.WaveFormat;
            _bytesPerFrame = _waveFormat.ConvertLatencyToByteSize(recorder.BufferMilliseconds);

            try
            {
                _echoCanceler = new SpeexDSPEchoCanceler(
                    recorder.BufferMilliseconds * _waveFormat.SampleRate / 1000, 
                    FilterLengthMs * _waveFormat.SampleRate / 1000, 
                    _waveFormat.Channels, 
                    player.OutputWaveFormat.Channels);

                var sampleRate = _waveFormat.SampleRate;
                _echoCanceler.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to intialize {nameof(SpeexDspEchoCanceler)}.", ex);
            }
        }

        public void EchoCancel(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (!Enabled) return;
            if (_echoCanceler == null || _waveFormat == null)
            {
                throw new InvalidOperationException("Speex echo canceller must be intialized with a recorder!");
            }
            if (buffer.Length < _bytesPerFrame)
            {
                throw new InvalidOperationException($"Input buffer must be {_bytesPerFrame} in length or higher!");
            }

            var outputBuffer = new byte[buffer.Length];
            _echoCanceler.EchoCapture(buffer, outputBuffer);
            outputBuffer.CopyTo(buffer);
        }

        public void EchoCancel(byte[] buffer) => EchoCancel(buffer.AsSpan());

        public void EchoPlayback(Span<byte> buffer) //Allow playback buffer to put in data regardless if enabled or not.
        {
            ThrowIfDisposed();

            if (_echoCanceler == null || _waveFormat == null)
            {
                throw new InvalidOperationException("Speex echo canceller must be intialized with a recorder!");
            }

            _echoCanceler.EchoPlayback(buffer);
        }

        public void EchoPlayback(byte[] buffer) => EchoPlayback(buffer.AsSpan());

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
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(SpeexDspEchoCanceler));
        }
    }
}