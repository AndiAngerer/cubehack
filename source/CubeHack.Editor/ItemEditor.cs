﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using CubeHack.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CubeHack.Editor
{
    public class ItemEditor : Control
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            "Item",
            typeof(Item),
            typeof(ItemEditor));

        public ItemEditor()
        {
        }

        public Item Item
        {
            get
            {
                return (Item)GetValue(ItemProperty);
            }

            set
            {
                SetValue(ItemProperty, value);
            }
        }
    }
}
