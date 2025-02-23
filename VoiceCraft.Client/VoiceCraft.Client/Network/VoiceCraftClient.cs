using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using VoiceCraft.Core;
using VoiceCraft.Core.ECS;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClient : IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);
        public static readonly WaveFormat WaveFormat = new(AudioConstants.SampleRate, AudioConstants.Channels);
        private static readonly uint BytesPerFrame = (uint)WaveFormat.ConvertLatencyToByteSize(AudioConstants.FrameSizeMs);

        public int Latency { get; private set; }
        public ConnectionStatus ConnectionStatus { get; private set; }

        //Network Events
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;

        //Packet Events
        public event Action<InfoPacket>? OnInfoPacketReceived;
        public event Action<SetLocalEntityPacket>? OnSetLocalEntityPacketReceived;
        public event Action<EntityCreatedPacket>? OnEntityCreatedPacketReceived;
        public event Action<EntityDestroyedPacket>? OnEntityDestroyedPacketReceived;
        
        //Client Events
        public event Action<Entity>? OnEntityCreated;
        public event Action<Entity>? OnEntityDestroyed;
        
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter;
        private readonly OpusEncoder _encoder;
        private readonly BufferedWaveProvider _queuedAudio;
        private readonly byte[] _extractedAudioBuffer;
        private readonly byte[] _encodedAudioBuffer;
        private readonly World _world;
        private int? _localEntityId;
        private NetPeer? _serverPeer;
        private bool _isDisposed;
        private uint _currentTimestamp;

        public VoiceCraftClient()
        {
            _dataWriter = new NetDataWriter();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            _encoder = new OpusEncoder(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            _queuedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = false, BufferDuration = TimeSpan.FromSeconds(2) }; //2 seconds.
            _extractedAudioBuffer = new byte[BytesPerFrame];
            _encodedAudioBuffer = new byte[1000];
            _world = new World();

            _listener.PeerConnectedEvent += OnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        private Entity CreateEntity(int entityServerId)
        {
            var entity = _world.CreateEntity(entityServerId);
            OnEntityCreated?.Invoke(entity);
            return entity;
        }
        
        private void DestroyEntity(int entityServerId)
        {
            var entity = _world.DestroyEntity(entityServerId);
            if(entity != null)
                OnEntityDestroyed?.Invoke(entity);
        }
        
        #region Public Methods
        public void Connect(string ip, int port, LoginType loginType)
        {
            ThrowIfDisposed();
            if (ConnectionStatus != ConnectionStatus.Disconnected)
                throw new InvalidOperationException("You must disconnect before connecting!");

            if (!_netManager.IsRunning)
                _netManager.Start();

            ConnectionStatus = ConnectionStatus.Connecting;
            var loginPacket = new LoginPacket()
            {
                Version = Version.ToString(),
                LoginType = loginType,
                PositioningType = PositioningType.Server
            };
            _dataWriter.Reset();
            loginPacket.Serialize(_dataWriter);
            _netManager.Connect(ip, port, _dataWriter);
        }

        public void Update()
        {
            _netManager.PollEvents();
            if (_queuedAudio.BufferedBytes < BytesPerFrame) return;
            Array.Clear(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            Array.Clear(_encodedAudioBuffer, 0, _encodedAudioBuffer.Length);
            _queuedAudio.Read(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            _currentTimestamp += AudioConstants.SamplesPerFrame;
            //Turns out RTPIncrement is samples per frame. IDK how this shit works...
            var encodedBytes = _encoder.Encode(_extractedAudioBuffer, AudioConstants.SamplesPerFrame, _encodedAudioBuffer, _encodedAudioBuffer.Length);
            //We don't need to input our public ID.
            SendPacket(new AudioPacket() { Timestamp = _currentTimestamp, Data = _encodedAudioBuffer, DataLength = encodedBytes },
                DeliveryMethod.Unreliable);
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            if (ConnectionStatus == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Must be connecting or connected before disconnecting!");
            
            _netManager.DisconnectAll();
            Update();
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (_serverPeer?.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            _serverPeer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        public void WriteAudio(byte[] buffer, int length)
        {
            _queuedAudio.AddSamples(buffer, 0, length);
        }
        #endregion
        
        #region Network Events
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            ConnectionStatus = ConnectionStatus.Connected;
            _serverPeer = peer;
            OnConnected?.Invoke();
        }

        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ConnectionStatus = ConnectionStatus.Disconnected;
            _serverPeer = null;
            OnDisconnected?.Invoke(disconnectInfo);
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Login:
                    break;
                case PacketType.Info:
                    var infoPacket = new InfoPacket();
                    infoPacket.Deserialize(reader);
                    OnInfoReceived(infoPacket);
                    break;
                case PacketType.SetLocalEntity:
                    var localEntityPacket = new SetLocalEntityPacket();
                    localEntityPacket.Deserialize(reader);
                    OnSetLocalEntityReceived(localEntityPacket);
                    break;
                case PacketType.EntityCreated:
                    var entityCreatedPacket = new EntityCreatedPacket();
                    entityCreatedPacket.Deserialize(reader);
                    OnEntityCreatedReceived(entityCreatedPacket);
                    break;
                case PacketType.EntityDestroyed:
                    var entityDestroyedPacket = new EntityDestroyedPacket();
                    entityDestroyedPacket.Deserialize(reader);
                    OnEntityDestroyedReceived(entityDestroyedPacket);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            reader.Recycle();
        }

        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            Latency = latency;
        }

        private static void OnConnectionRequestEvent(ConnectionRequest request)
        {
            request.Reject();
        }
        #endregion
        
        #region Packet Events
        private void OnInfoReceived(InfoPacket infoPacket)
        {
            OnInfoPacketReceived?.Invoke(infoPacket);
        }
        
        private void OnSetLocalEntityReceived(SetLocalEntityPacket packet)
        {
            if(_localEntityId == null)
                CreateEntity(packet.Id);
            _localEntityId = packet.Id;
            OnSetLocalEntityPacketReceived?.Invoke(packet);
        }

        private void OnEntityCreatedReceived(EntityCreatedPacket packet)
        {
            CreateEntity(packet.Id);
            Debug.WriteLine($"Created Entity with ID {packet.Id}");
            OnEntityCreatedPacketReceived?.Invoke(packet);
        }
        
        private void OnEntityDestroyedReceived(EntityDestroyedPacket packet)
        {
            DestroyEntity(packet.Id);
            Debug.WriteLine($"Destroyed Entity with ID {packet.Id}");
            OnEntityDestroyedPacketReceived?.Invoke(packet);   
        }
        #endregion

        #region Dispose
        private void ThrowIfDisposed()
        {
            if (!_isDisposed) return;
            throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                _encoder.Dispose();
                _queuedAudio.ClearBuffer();
            }

            _isDisposed = true;
        }
        #endregion
    }
}