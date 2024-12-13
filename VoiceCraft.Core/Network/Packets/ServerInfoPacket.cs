using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class ServerInfoPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.ServerInfo;
        public string Motd { get; set; } = string.Empty;
        public uint ConnectedPlayers { get; set; }
        public PositioningMode PositioningMode { get; set; }
        public bool DiscoveryEnabled  { get; set; }
        
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Motd);
            writer.Put(ConnectedPlayers);
            writer.Put((byte)PositioningMode);
            writer.Put(DiscoveryEnabled);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Motd = reader.GetString();
            ConnectedPlayers = reader.GetUInt();
            PositioningMode = (PositioningMode)reader.GetByte();
            DiscoveryEnabled = reader.GetBool();
        }
    }
}