using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Name);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetGuid();
            Name = reader.GetString();
        }
    }
}