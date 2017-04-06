// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Windows;
using System.Windows.Controls;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for DeviceListItem.xaml
    /// </summary>
    public partial class DeviceListItem : UserControl
    {
        public delegate void CheckedHandler(DeviceListItem item, bool value);
        public event CheckedHandler CheckedChanged;


        public string DeviceId { get; set; }

        public bool SuppressCheckedChanged { get; set; }

        public Api.v2.Data.ConnectionDefinition Connection { get; set; }

        public bool Checked
        {
            get { return (bool)GetValue(CheckedProperty); }
            set { SetValue(CheckedProperty, value); }
        }

        public static readonly DependencyProperty CheckedProperty =
            DependencyProperty.Register("Checked", typeof(bool), typeof(DeviceListItem), new PropertyMetadata(false, new PropertyChangedCallback(Checked_PropertyChanged)));

        private static void Checked_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as DeviceListItem;
            if (o != null) o.UpdateChecked();
        }

        protected void UpdateChecked()
        {
            if (!SuppressCheckedChanged) CheckedChanged?.Invoke(this, Checked);
        }


        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set { SetValue(DeviceNameProperty, value); }
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register("DeviceName", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Port
        {
            get { return (string)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Manufacturer
        {
            get { return (string)GetValue(ManufacturerProperty); }
            set { SetValue(ManufacturerProperty, value); }
        }

        public static readonly DependencyProperty ManufacturerProperty =
            DependencyProperty.Register("Manufacturer", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Model
        {
            get { return (string)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public string Serial
        {
            get { return (string)GetValue(SerialProperty); }
            set { SetValue(SerialProperty, value); }
        }

        public static readonly DependencyProperty SerialProperty =
            DependencyProperty.Register("Serial", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));


        public DeviceListItem()
        {
            Init();
        }

        public DeviceListItem(Api.v2.Data.ConnectionDefinition connection)
        {
            Init();
            Connection = connection;
            DeviceId = connection.DeviceId;
            Address = connection.Address;
            Port = connection.Port.ToString();
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
