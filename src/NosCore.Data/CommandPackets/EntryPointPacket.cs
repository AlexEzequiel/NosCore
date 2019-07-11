﻿using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets;
using ChickenAPI.Packets.Attributes;

namespace NosCore.Data.CommandPackets
{
    [PacketHeader("EntryPoint", AnonymousAccess = true)]
    public class EntryPointPacket : PacketBase
    {
        [PacketIndex(0)]
        public string Title { get; set; }

        [PacketIndex(1)]
        public string Packet1Id { get; set; }

        [PacketIndex(2)]
        public string Name { get; set; }

        [PacketIndex(3)]
        public string Packet2Id { get; set; }

        [PacketIndex(4)]
        public string Password { get; set; }
    }
}