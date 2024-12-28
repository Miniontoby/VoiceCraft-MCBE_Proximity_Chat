using System.Collections.Generic;
using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioOutput
    {
        int Read(byte[] buffer, int offset, int count);
        void GetVisibleEntities(List<Entity> entities);
    }
}