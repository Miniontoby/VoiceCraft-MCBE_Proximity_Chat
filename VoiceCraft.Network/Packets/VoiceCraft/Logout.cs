﻿using System.Text;

namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class Logout : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Logout;
        public override bool IsReliable => false;

        //Packet Variables
        public long Id { get; set; } = long.MinValue;
        public string Reason { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            var reasonLength = BitConverter.ToInt32(dataStream, offset); //Read Reason length - 4 bytes.
            offset += sizeof(int);

            if (reasonLength > 0)
                Reason = Encoding.UTF8.GetString(dataStream, offset, reasonLength); //Read Reason.

            offset += reasonLength;
            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(Reason.Length));
            if (Reason.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Reason));
        }
    }
}