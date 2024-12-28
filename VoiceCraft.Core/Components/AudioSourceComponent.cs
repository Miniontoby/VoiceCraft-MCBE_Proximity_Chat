using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : IAudioOutput, IComponent<AudioSourceComponent>
    {
        private IAudioInput? _audioInput;
        public event Action<AudioSourceComponent>? OnUpdate;
        public event Action<AudioSourceComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public IAudioInput? AudioInput
        {
            get => _audioInput;
            set
            {
                if (value == _audioInput) return;
                _audioInput = value;
                OnUpdate?.Invoke(this);
            }
        }
        
        public AudioSourceComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void GetVisibleEntities(List<Entity> entities)
        {
            if (entities.Contains(Entity)) return;
            entities.Add(Entity);
            _audioInput?.GetVisibleEntities(entities);
        }
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}