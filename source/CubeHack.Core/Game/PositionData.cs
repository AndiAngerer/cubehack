﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using ProtoBuf;

namespace CubeHack.Game
{
    [ProtoContract]
    public class PositionData
    {
        [ProtoMember(1)]
        public Position Position;

        [ProtoMember(2)]
        public Offset Velocity;

        [ProtoMember(3)]
        public float HAngle;

        [ProtoMember(4)]
        public float VAngle;

        [ProtoMember(5)]
        public bool IsFalling;

        [ProtoMember(6)]
        public Position CollisionPosition;
    }
}
