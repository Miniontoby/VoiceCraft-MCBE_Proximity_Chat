using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityDestroyedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityDestroyed;
        public int NetworkId { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
        }
    }
}