using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using LiteNetLib;
using LiteNetLib.Utils;
using Schedulers;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.EventHandlers;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private const int PINGER_BROADCAST_INTERVAL_MS = 5000;
        public static readonly Version Version = new(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;

        //Public Properties
        public ServerProperties Properties { get; }
        public EventBasedNetListener Listener { get; }
        public World World { get; } = World.Create();
        
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter = new();
        private readonly NetworkEventHandler _networkEventHandler;
        private readonly Group<float> _systems;
        private readonly JobScheduler _jobScheduler;
        private bool _isDisposed;
        private int _lastPingBroadcast = Environment.TickCount;

        public VoiceCraftServer(ServerProperties? properties = null)
        {
            Properties = properties ?? new ServerProperties();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true
            };
            
            _networkEventHandler = new NetworkEventHandler(this, _netManager);
            _systems = new Group<float>("systems", new NetworkSystem(World, this));
            _jobScheduler = new JobScheduler(
                new JobScheduler.Config
                {
                    ThreadPrefixName = "VoiceCraft.Server",
                    ThreadCount = 0,                           // 0 = Determine at runtime
                    MaxExpectedConcurrentJobs = 64,
                    StrictAllocationMode = false,
                }
            );
            World.SharedJobScheduler = _jobScheduler;
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods

        public void Start(int port)
        {
            if (_netManager.IsRunning) return;
            _netManager.Start(port);
            _systems.Initialize();
            OnStarted?.Invoke();
        }

        public void Update(float deltaTime)
        {
            _netManager.PollEvents();
            World.TrimExcess();
            
            _systems.BeforeUpdate(deltaTime);
            _systems.Update(deltaTime);
            _systems.AfterUpdate(deltaTime);
            
            if (Environment.TickCount - _lastPingBroadcast < PINGER_BROADCAST_INTERVAL_MS) return;
            _lastPingBroadcast = Environment.TickCount;
            var serverInfoPacket = new InfoPacket()
            {
                Motd = Properties.Motd,
                Clients = (uint)_netManager.ConnectedPeersCount,
                Discovery = Properties.Discovery,
                PositioningType = Properties.PositioningType,
            };

            SendPacket(
                _netManager.ConnectedPeerList
                    .Where(peer => peer.ConnectionState == ConnectionState.Connected && (LoginType?)peer.Tag == LoginType.Pinger).ToArray(),
                serverInfoPacket);
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

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
        }

        public Entity CreateEntity()
        {
            var entity = this.World.Create();
            WorldEventHandler.InvokeEntityCreated(new EntityCreatedEvent(entity));
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            var entityComponents = entity.GetAllComponents(); //Get all entity components before destroying the entity.
            World.Destroy(entity);
            foreach (var component in entityComponents)
            {
                if(component is IDisposable disposable) //Disposable
                    disposable.Dispose(); //Dispose events will still trigger ComponentRemoved events.
            }
            WorldEventHandler.InvokeEntityDestroyed(new EntityDestroyedEvent(entity));
        }

        #endregion

        #region Dispose
        
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                _systems.Dispose();
                _jobScheduler.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}