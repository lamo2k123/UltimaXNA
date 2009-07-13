﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltimaXNA.Network.Packets.Server
{
    public class SupportedFeaturesPacket : RecvPacket
    {
        readonly short _flags;

        /// <summary>
        /// 0x01: enable T2A features: chat, regions
        /// 0x02: enable renaissance features
        /// 0x04: enable third dawn features
        /// 0x08: enable LBR features: skills, map
        /// 0x10: enable AOS features: skills, map, spells, fightbook
        /// 0x20: 6th character slot
        /// 0x40: enable SE features
        /// 0x80: enable ML features: elven race, spells, skills
        /// 0x100: enable 8th age splash screen
        /// 0x200: enable 9th age splash screen
        /// 0x1000: 7th character slot
        /// 0x2000: 10th age KR faces
        /// </summary>
        public short Flags
        {
            get { return _flags; }
        }

        public SupportedFeaturesPacket(PacketReader reader)
            : base(0xB9, "Enable Features")
        {
            _flags = reader.ReadInt16();
        }
    }
}