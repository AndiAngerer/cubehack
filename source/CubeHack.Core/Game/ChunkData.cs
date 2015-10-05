﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using ProtoBuf;

namespace CubeHack.Game
{
    [ProtoContract]
    public class ChunkData
    {
        [ProtoMember(1)]
        public int X { get; set; }

        [ProtoMember(2)]
        public int Y { get; set; }

        [ProtoMember(3)]
        public int Z { get; set; }

        [ProtoMember(4)]
        public byte[] Data { get; set; }
    }
}
