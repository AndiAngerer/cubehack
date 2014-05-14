﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace CubeHack.GameData
{
    [ProtoContract]
    [ContentProperty("Color")]
    public class SolidColorLayer : TextureLayer
    {
        [ProtoMember(1)]
        public Color Color { get; set; }
    }
}
