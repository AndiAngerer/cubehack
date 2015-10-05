﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using ProtoBuf;

namespace CubeHack.Data
{
    [EditorData]
    [ProtoContract]
    public class MobType
    {
        [EditorData]
        [ProtoMember(1)]
        public string Name { get; set; }

        [EditorData]
        [ProtoMember(2)]
        public Model Model { get; set; }
    }
}
