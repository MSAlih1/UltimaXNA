﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UltimaXNA.Core.Network.Packets;

namespace UltimaXNA.Ultima.Network.Client
{
    public class PartyQueryLocs : SendPacket
    {
        public PartyQueryLocs() : base(240, "Query Party Locations")
        {
            Stream.Write((byte)0);
        }
    }
}