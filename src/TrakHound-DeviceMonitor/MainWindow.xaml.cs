// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using TrakHound.Api.v2.Requests;
using System.Threading;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<DeviceItem> _deviceItems;
        public ObservableCollection<DeviceItem> DeviceItems
        {
            get
            {
                if (_deviceItems == null) _deviceItems = new ObservableCollection<DeviceItem>();
                return _deviceItems;
            }
            set
            {
                _deviceItems = value;
            }
        }

        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            DockRight();

            LoadDevices();
        }

        private void LoadDevices()
        {
            Loading = true;

            // Stop Devices in List
            foreach (var item in DeviceItems) item.Stop();

            // Clear List
            DeviceItems.Clear();

            // Get Devices from Api
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var connections = Connections.Get("http://localhost");
                foreach (var connection in connections)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var deviceItem = new DeviceItem(connection);
                        deviceItem.Start();
                        DeviceItems.Add(deviceItem);
                    }));
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Loading = false;
                }));
            });

        }

        private void DockRight()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
         
            Width = 300;
            Height = screenHeight - 50;

            Top = 0;
            Left = screenWidth - Width;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new About();
            about.Owner = this;
            about.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var deviceItem in DeviceItems) deviceItem.Stop();
        }

        private void Refresh_Clicked(TrakHound_UI.Button bt)
        {
            LoadDevices();
        }
    }
}
