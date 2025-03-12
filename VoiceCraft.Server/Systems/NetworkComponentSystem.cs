using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.Systems
{
    public class NetworkComponentSystem : BaseSystem<World, float>
    {
        private readonly VoiceCraftServer _server;
        private readonly List<object> _visibleComponents = [];
        private readonly List<Entity> _uniqueEntities = [];
        private readonly QueryDescription _query = new QueryDescription().WithAll<NetworkComponent>();

        public NetworkComponentSystem(World world, VoiceCraftServer server) : base(world)
        {
            _server = server;

            WorldEventHandler.OnComponentAdded += OnComponentAdded;
            WorldEventHandler.OnComponentUpdated += OnComponentUpdated;
            WorldEventHandler.OnComponentRemoved += OnComponentRemoved;
        }

        public override void Update(in float deltaTime)
        {
            World.Query(in _query, (ref NetworkComponent networkComponent) =>
            {
                //This needs optimisation! I don't know how though!!..
                networkComponent.ClearDeadVisibleEntities();
                _visibleComponents.Clear();
                _uniqueEntities.Clear();
            
                var components = networkComponent.Entity.GetAllComponents();
                foreach (var component in components)
                {
                    if (component is not IVisibleComponent visibleComponent) continue;
                    visibleComponent.GetVisibleComponents(World, _visibleComponents);
                }
            
                foreach (var visibleComponent in _visibleComponents)
                {
                    if (visibleComponent is not IEntityComponent entityComponent || _uniqueEntities.Contains(entityComponent.Entity)) continue;
                    _uniqueEntities.Add(entityComponent.Entity);
                }

                var newVisibleEntities =
                    (from uniqueEntity in _uniqueEntities where uniqueEntity.Has<NetworkComponent>() select uniqueEntity.Get<NetworkComponent>()).ToList();
                networkComponent.SetVisibleEntities(newVisibleEntities);
            });
        }

        private void OnComponentAdded(ComponentAddedEvent @event)
        {
            if (@event.Component is NetworkComponent networkComponent)
            {
                var entityCreatedPacket = new EntityCreatedPacket() { NetworkId = networkComponent.NetworkId };
                var componentAddedPacket = new AddComponentPacket() { NetworkId = networkComponent.NetworkId };
                var components = @event.Component.Entity.GetAllComponents();

                Broadcast(entityCreatedPacket, networkComponent.NetPeer);
                foreach (var component in components)
                {
                    if (component is not ISerializableEntityComponent serializableEntityComponent) continue;
                    componentAddedPacket.ComponentType = serializableEntityComponent.ComponentType;
                    Broadcast(componentAddedPacket);
                }
            }
            else if (@event.Component.Entity.Has<NetworkComponent>() && @event.Component is ISerializableEntityComponent serializableEntityComponent)
            {
                var entityNetworkComponent = @event.Component.Entity.Get<NetworkComponent>();
                var componentAddedPacket = new AddComponentPacket()
                {
                    NetworkId = entityNetworkComponent.NetworkId,
                    ComponentType = serializableEntityComponent.ComponentType
                };
                //Notify all entities of the changes.
                Broadcast(componentAddedPacket);
            }
        }

        private void OnComponentUpdated(ComponentUpdatedEvent @event)
        {
            if (!@event.Component.Entity.Has<NetworkComponent>() || @event.Component is not ISerializableEntityComponent serializableEntityComponent) return;
            var networkComponent = @event.Component.Entity.Get<NetworkComponent>();
            var componentUpdatedPacket = new UpdateComponentPacket()
            {
                NetworkId = networkComponent.NetworkId,
                ComponentType = serializableEntityComponent.ComponentType,
                Data = serializableEntityComponent.Serialize()
            };
            foreach (var visibleEntity in networkComponent.VisibleNetworkEntities)
            {
                if (visibleEntity.NetPeer == null) continue;
                _server.SendPacket(visibleEntity.NetPeer, componentUpdatedPacket);
            }
        }

        private void OnComponentRemoved(ComponentRemovedEvent @event)
        {
            if (@event.Component is NetworkComponent networkComponent)
            {
                //Disconnect local client. This should only happen when the server destroys the entity or for some reason. I decide to just allow removal of the
                //network component BECAUSE WHY THE FUCK NOT?
                networkComponent.NetPeer?.Disconnect();

                var entityDestroyedPacket = new EntityDestroyedPacket() { NetworkId = networkComponent.NetworkId };
                Broadcast(entityDestroyedPacket);
            }
            else if (@event.Component.Entity.Has<NetworkComponent>() && @event.Component is ISerializableEntityComponent serializableEntityComponent)
            {
                var entityNetworkComponent = @event.Component.Entity.Get<NetworkComponent>();
                var componentRemovedPacket = new RemoveComponentPacket()
                {
                    NetworkId = entityNetworkComponent.NetworkId,
                    ComponentType = serializableEntityComponent.ComponentType
                };
                //Notify all entities of the changes.
                Broadcast(componentRemovedPacket);
            }
        }

        private void Broadcast(VoiceCraftPacket packet, params NetPeer?[] excludedPeers)
        {
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();

            World.Query(in query, (ref NetworkComponent netComponent) =>
            {
                if (netComponent.NetPeer == null || excludedPeers.Contains(netComponent.NetPeer)) return; //not a client or is excluded.
                _server.SendPacket(netComponent.NetPeer, packet);
            });
        }
    }
}