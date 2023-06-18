﻿namespace VoiceCraft.Mobile.Models
{
    public class ServerModel
    {
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public ushort Port { get; set; } = 9050;
        public ushort Key { get; set; }
        public bool ClientSided { get; set; }
    }
}
