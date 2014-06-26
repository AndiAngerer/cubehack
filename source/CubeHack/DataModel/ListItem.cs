﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CubeHack.DataModel
{
    public class ListItem : Item
    {
        readonly Type _valueType;

        public ListItem(Type valueType)
        {
            _valueType = valueType;
            Entries = new ObservableCollection<Entry>();
            AddEntryCommand = new Command(AddEntry);
            IsExpanded = true;
        }

        public ICommand AddEntryCommand
        {
            get;
            private set;
        }

        ObservableCollection<Entry> _entries;
        public ObservableCollection<Entry> Entries
        {
            get
            {
                return _entries;
            }

            private set
            {
                SetAndNotify(ref _entries, value);
            }
        }

        public override JToken Save()
        {
            var output = new JArray();

            foreach (var entry in Entries)
            {
                output.Add(entry.Value.Save());
            }

            return output;
        }

        public override void Load(JToken data)
        {
            JArray o = data as JArray;
            if (o == null)
            {
                return;
            }

            foreach (var entry in o)
            {
                var item = Item.Create(_valueType);
                item.Load(entry);
                Entries.Add(new Entry(this) { Value = item });
            }

            IsExpanded = Entries.Count <= ExpansionLimit;
        }

        public override object GetObject()
        {
            System.Collections.IList instance = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_valueType));
            foreach (var entry in Entries)
            {
                instance.Add(entry.Value.GetObject());
            }
            return instance;
        }

        void AddEntry()
        {
            Entries.Add(new Entry(this) { Value = Item.Create(_valueType) });
        }

        public class Entry : NotifyPropertyChanged
        {
            private ListItem _parent;

            public Entry(ListItem parent)
            {
                _parent = parent;
                RemoveCommand = new Command(Remove);
            }

            public ICommand RemoveCommand
            {
                get;
                private set;
            }

            private Item _value;
            public Item Value
            {
                get { return _value; }
                set { SetAndNotify(ref _value, value); }
            }

            private void Remove()
            {
                _parent.Entries.Remove(this);
            }
        }
    }
}