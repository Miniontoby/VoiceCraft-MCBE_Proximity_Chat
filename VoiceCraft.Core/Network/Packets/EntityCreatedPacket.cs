using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public int Id { get; set; }
        public int WorldId { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(WorldId);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            WorldId = reader.GetInt();
        }
    }
}