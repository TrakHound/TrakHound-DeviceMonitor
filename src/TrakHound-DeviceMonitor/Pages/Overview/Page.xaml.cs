// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;

namespace TrakHound.DeviceMonitor.Pages.Overview
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// </summary>
    public partial class Page : UserControl
    {
        private bool started;

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public delegate void IndexChanged_Handler(string deviceId, int index);
        public event IndexChanged_Handler IndexChanged;

        private ObservableCollection<DevicePanel> _panels;
        public ObservableCollection<DevicePanel> Panels
        {
            get
            {
                if (_panels == null) _panels = new ObservableCollection<DevicePanel>();
                return _panels;
            }
            set
            {
                _panels = value;
            }
        }


        public double ZoomLevel
        {
            get { return (double)GetValue(ZoomLevelProperty); }
            set { SetValue(ZoomLevelProperty, value); }
        }

        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register("ZoomLevel", typeof(double), typeof(Page), new PropertyMetadata(1d));



        public Page()
        {
            InitializeComponent();
            root.DataContext = this;
        }

        public void Start()
        {
            if (!started)
            {
                foreach (var panel in Panels) panel.Start(From, To);

                started = true;
            }
        }

        public void Stop()
        {
            if (started)
            {
                foreach (var panel in Panels) panel.Stop();

                started = false;
            }
        }

        public void AddDevice(DeviceModel model, int index)
        {
            if (!Panels.ToList().Exists(o => o.DeviceId == model.DeviceId))
            {
                // Create Device Panel
                var panel = new DevicePanel(model);
                panel.Index = index;
                panel.IndexChanged += Panel_IndexChanged;
                Panels.Add(panel);
                Panels.Sort();
                if (started) panel.Start(From, To);
            }
        }

        public void RemoveDevice(string deviceId)
        {
            var index = Panels.ToList().FindIndex(o => o.DeviceId == deviceId);
            if (index >= 0)
            {
                Panels[index].Stop();
                Panels.RemoveAt(index);
            }
        }

        public void ClearDevices()
        {
            foreach (var panel in Panels) panel.Stop();
            Panels.Clear();
        }

        public void SortDevices()
        {
            Panels.Sort();
        }

        public void UpdateTimespan(DateTime from, DateTime to)
        {
            From = from;
            To = to;

            if (started)
            {
                foreach (var panel in Panels) panel.UpdateTimespan(from, to);
            }
        }

        public void UpdateDeviceIndex(string deviceId, int index)
        {
            int i = Panels.ToList().FindIndex(o => o.DeviceId == deviceId);
            if (i >= 0)
            {
                bool moveUp = Panels[i].Index > index;
                Panels[i].Index = index;

                Console.WriteLine("UpdateDeviceIndex() : " + deviceId + " = " + index + " : Changed");

                foreach (var panel in Panels)
                {
                    if (panel.DeviceId != deviceId)
                    {
                        if (moveUp)
                        {
                            if (panel.Index >= index)
                            {
                                //Console.WriteLine("UpdateDeviceIndex() : " + panel.DeviceId + " = " + panel.Index + " : BEFORE");
                                panel.Index++;
                            }
                        }
                        else
                        {
                            if (panel.Index <= index)
                            {
                                //Console.WriteLine("UpdateDeviceIndex() : " + panel.DeviceId + " = " + panel.Index + " : BEFORE");
                                panel.Index--;
                            }
                        }

                        Console.WriteLine("UpdateDeviceIndex() : " + panel.DeviceId + " = " + panel.Index + " : AFTER");
                    }
                }

                Panels.Sort();
            }
        }

        private void Panel_IndexChanged(string deviceId, int index)
        {
            IndexChanged?.Invoke(deviceId, index);
        }
    }
}
