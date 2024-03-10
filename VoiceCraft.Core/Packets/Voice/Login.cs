﻿using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Login : IPacketData
    {
        public int PrivateId { get; set; }

        public Login(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //read login key - 4 bytes.
        }

        public Login()
        {
            PrivateId = 0;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));

            return dataStream.ToArray();
        }

        public static VoicePacket Create(int privateId)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.Login,
                PacketData = new Login()
                {
                    PrivateId = privateId
                }
            };
        }
    }
}
