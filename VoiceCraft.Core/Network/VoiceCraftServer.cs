using System;
using System.Linq;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private const int PINGER_BROADCAST_INTERVAL_MS = 5000;

        public static readonly Version Version = new Version(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;
        public event Action<NetPeer>? OnClientConnected;
        public event Action<NetPeer, DisconnectInfo>? OnClientDisconnected;

        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public bool DiscoveryEnabled { get; set; }
        public PositioningType PositioningType { get; set; }

        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor;
        private readonly CancellationTokenSource _cts;
        private readonly NetDataWriter _dataWriter;
        private bool _isDisposed;
        private int _lastPingBroadcast = Environment.TickCount;

        public VoiceCraftServer()
        {
            _dataWriter = new NetDataWriter();
            _packetProcessor = new NetPacketProcessor();
            _cts = new CancellationTokenSource();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true
            };

            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.PeerConnectedEvent += ListenerOnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += ListenerOnPeerDisconnectedEvent;
            _listener.NetworkReceiveEvent += ListenerOnNetworkReceiveEvent;
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        //Public Methods
        public void Start(int port)
        {
            if (_netManager.IsRunning) return;
            _netManager.Start(port);
            OnStarted?.Invoke();
        }

        public void Update()
        {
            _netManager.PollEvents();

            if (Environment.TickCount - _lastPingBroadcast < PINGER_BROADCAST_INTERVAL_MS) return;
            _lastPingBroadcast = Environment.TickCount;
            var serverInfoPacket = new ServerInfoPacket()
            {
                Motd = Motd,
                Discovery = DiscoveryEnabled,
                PositioningType = PositioningType,
            };

            SendPacket(
                _netManager.ConnectedPeerList
                    .Where(peer => peer.ConnectionState == ConnectionState.Connected && (LoginType?)peer.Tag == LoginType.Pinger).ToArray(),
                serverInfoPacket);
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
        }

        public bool SendPacket<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (peer.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            peer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        public bool SendPacket<T>(NetPeer[] peers, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            var status = true;
            foreach (var peer in peers)
            {
                status = SendPacket(peer, packet, deliveryMethod);
            }

            return status;
        }

        //Events
        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            if (request.Data.IsNull)
            {
                request.Reject();
                return;
            }

            try
            {
                var loginPacket = new LoginPacket();
                loginPacket.Deserialize(request.Data);

                if (Version.Parse(loginPacket.Version).Major != Version.Major)
                {
                    request.Reject();
                    return;
                }
                
                switch (loginPacket.LoginType)
                {
                    case LoginType.Pinger:
                    case LoginType.Login:
                    case LoginType.Discovery:
                        var peer = request.Accept();
                        peer.Tag = loginPacket.LoginType;
                        break;
                    default:
                        request.Reject();
                        break;
                }
            }
            catch
            {
                request.Reject();
            }
        }

        private void ListenerOnPeerConnectedEvent(NetPeer peer)
        {
            OnClientConnected?.Invoke(peer);
            if ((LoginType?)peer.Tag == LoginType.Pinger)
            {
                var serverInfoPacket = new ServerInfoPacket()
                {
                    Motd = Motd,
                    Discovery = DiscoveryEnabled,
                    PositioningType = PositioningType,
                };
                SendPacket(peer, serverInfoPacket);
            }
        }

        private void ListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            OnClientDisconnected?.Invoke(peer, disconnectinfo);
        }

        private void ListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            _packetProcessor.ReadAllPackets(reader, peer);
            reader.Recycle();
        }

        //Packet Events

        //Dispose
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                _cts.Cancel();
                _cts.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}