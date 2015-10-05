﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using CubeHack.EditorModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace CubeHack.Editor
{
    public class ViewModel : NotifyPropertyChanged
    {
        private string _modName;

        private Item _modItem;

        public ViewModel()
        {
            SaveCommand = new Command(Save);
            RunCommand = new Command(Run);

            ModName = "Core";
            ModItem = Item.Create(typeof(CubeHack.Data.Mod));
            ModItem.IsExpanded = true;
            Load();
        }

        public event Action TriggerFocusChange;

        public ICommand SaveCommand { get; private set; }

        public ICommand RunCommand { get; private set; }

        public string ModName
        {
            get { return _modName; }
            set { SetAndNotify(ref _modName, value); }
        }

        public Item ModItem
        {
            get { return _modItem; }
            set { SetAndNotify(ref _modItem, value); }
        }

        private static string GetDirectory()
        {
            return Path.GetDirectoryName(typeof(ViewModel).Assembly.Location);
        }

        private void Load()
        {
            string s = File.ReadAllText(GetPath(), Encoding.UTF8);
            var json = JToken.Parse(s);
            ModItem.Load(json);
        }

        private void Save()
        {
            if (TriggerFocusChange != null) TriggerFocusChange();

            string s = JsonConvert.SerializeObject(ModItem.Save(), Formatting.Indented);
            File.WriteAllText(GetPath(), s, Encoding.UTF8);
        }

        private void Run()
        {
            Save();
            System.Diagnostics.Process.Start(Path.Combine(GetDirectory(), "CubeHack.exe"), "localhost");
        }

        private string GetPath()
        {
            return Path.Combine(GetDirectory(), "Mods", _modName + ".cubemod.json");
        }
    }
}
