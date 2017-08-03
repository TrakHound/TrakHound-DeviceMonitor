// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;

namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for DeviceListItem.xaml
    /// </summary>
    public partial class DeviceListItem : UserControl
    {
        public delegate void CheckedHandler(DeviceListItem item, bool value);
        public event CheckedHandler CheckedChanged;


        public bool SuppressCheckedChanged { get; set; }

        public DeviceConfiguration Configuration { get; set; }

        public ConnectionDefinition Connection { get; set; }

        public DeviceModel Model { get; set; }

        public byte[] DeviceImageBytes { get; set; }

        public string DeviceId
        {
            get { return Configuration.DeviceId; }
        }

        #region "Dependency Properties"

        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.Register("Enabled", typeof(bool), typeof(DeviceListItem), new PropertyMetadata(false, new PropertyChangedCallback(Checked_PropertyChanged)));

        public bool PerformanceEnabled
        {
            get { return (bool)GetValue(PerformanceEnabledProperty); }
            set { SetValue(PerformanceEnabledProperty, value); }
        }

        public static readonly DependencyProperty PerformanceEnabledProperty =
            DependencyProperty.Register("PerformanceEnabled", typeof(bool), typeof(DeviceListItem), new PropertyMetadata(false, new PropertyChangedCallback(Checked_PropertyChanged)));

        public bool QualityEnabled
        {
            get { return (bool)GetValue(QualityEnabledProperty); }
            set { SetValue(QualityEnabledProperty, value); }
        }

        public static readonly DependencyProperty QualityEnabledProperty =
            DependencyProperty.Register("QualityEnabled", typeof(bool), typeof(DeviceListItem), new PropertyMetadata(false, new PropertyChangedCallback(Checked_PropertyChanged)));

        private static void Checked_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as DeviceListItem;
            if (o != null) o.UpdateChecked();
        }

        protected void UpdateChecked()
        {
            Configuration.Enabled = Enabled;
            Configuration.PerformanceEnabled = PerformanceEnabled;
            Configuration.QualityEnabled = QualityEnabled;

            if (!SuppressCheckedChanged) CheckedChanged?.Invoke(this, Enabled);
        }


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


        #region "Description"

        /// <summary>
        /// Description of the Device
        /// </summary>
        public string DeviceDescription
        {
            get { return (string)GetValue(DeviceDescriptionProperty); }
            set { SetValue(DeviceDescriptionProperty, value); }
        }

        public static readonly DependencyProperty DeviceDescriptionProperty =
            DependencyProperty.Register("DeviceDescription", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// MTConnect Device Name
        /// </summary>
        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set { SetValue(DeviceNameProperty, value); }
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register("DeviceName", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// MTConnect Device ID
        /// </summary>
        public string ID
        {
            get { return (string)GetValue(IDProperty); }
            set { SetValue(IDProperty, value); }
        }

        public static readonly DependencyProperty IDProperty =
            DependencyProperty.Register("ID", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// Manufacturer of the Device
        /// </summary>
        public string DeviceManufacturer
        {
            get { return (string)GetValue(DeviceManufacturerProperty); }
            set { SetValue(DeviceManufacturerProperty, value); }
        }

        public static readonly DependencyProperty DeviceManufacturerProperty =
            DependencyProperty.Register("DeviceManufacturer", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// Model Number of the Device
        /// </summary>
        public string DeviceModel
        {
            get { return (string)GetValue(DeviceModelProperty); }
            set { SetValue(DeviceModelProperty, value); }
        }

        public static readonly DependencyProperty DeviceModelProperty =
            DependencyProperty.Register("DeviceModel", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// Serial Number of the Device
        /// </summary>
        public string DeviceSerial
        {
            get { return (string)GetValue(DeviceSerialProperty); }
            set { SetValue(DeviceSerialProperty, value); }
        }

        public static readonly DependencyProperty DeviceSerialProperty =
            DependencyProperty.Register("DeviceSerial", typeof(string), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// Image of the Device
        /// </summary>
        public ImageSource DeviceImage
        {
            get { return (ImageSource)GetValue(DeviceImageProperty); }
            set { SetValue(DeviceImageProperty, value); }
        }

        public static readonly DependencyProperty DeviceImageProperty =
            DependencyProperty.Register("DeviceImage", typeof(ImageSource), typeof(DeviceListItem), new PropertyMetadata(null));

        /// <summary>
        /// Logo for the Device
        /// </summary>
        public ImageSource DeviceLogo
        {
            get { return (ImageSource)GetValue(DeviceLogoProperty); }
            set { SetValue(DeviceLogoProperty, value); }
        }

        public static readonly DependencyProperty DeviceLogoProperty =
            DependencyProperty.Register("DeviceLogo", typeof(ImageSource), typeof(DeviceListItem), new PropertyMetadata(null));

        #endregion

        #endregion


        public DeviceListItem(DeviceModel model)
        {
            Init();

            Model = model;

            // Connection Info
            if (model.Connection != null)
            {
                Configuration.DeviceId = model.DeviceId;
                Address = model.Connection.Address;
                Port = model.Connection.Port.ToString();
            }

            // Description Info
            ID = model.Id;
            DeviceName = model.Name;
            DeviceManufacturer = model.Manufacturer;
            DeviceModel = model.Model;
            DeviceSerial = model.SerialNumber;
            DeviceDescription = !string.IsNullOrEmpty(model.Description) ? model.Description.Trim() : null;

            // Download Images using the TrakHound Images Api
            if (!string.IsNullOrEmpty(model.Manufacturer))
            {
                // Get Device Logo
                var logoUrl = "https://images.trakhound.com/device-image?manufacturer=" + Model.Manufacturer;
                var logoRequest = new Task<byte[]>(() => DownloadImage(logoUrl));
                logoRequest.ContinueWith(imgRequest =>
                {
                    if (imgRequest != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var img = BitmapFromBytes(imgRequest.Result);
                            if (img != null) DeviceLogo = SetImageSize(img, 0, 25);
                        }), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
                    }
                });
                logoRequest.Start();

                // Get Device Model Image
                if (!string.IsNullOrEmpty(model.Model))
                {
                    var imageUrl = "https://images.trakhound.com/device-image?manufacturer=" + model.Manufacturer + "&model=" + model.Model;
                    var imageRequest = new Task<byte[]>(() => DownloadImage(imageUrl));
                    imageRequest.ContinueWith(imgRequest =>
                    {
                        if (imgRequest != null)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                var imgBytes = imgRequest.Result;

                                DeviceImageBytes = imgBytes;

                                var img = BitmapFromBytes(imgBytes);
                                if (img != null) DeviceImage = SetImageSize(img, 100, 80);
                            }), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
                        }
                    });
                    imageRequest.Start();
                }
            }
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;

            Configuration = new DeviceConfiguration();
            PerformanceEnabled = Configuration.PerformanceEnabled;
            QualityEnabled = Configuration.QualityEnabled;
        }

        private byte[] DownloadImage(string url)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest();
                return client.DownloadData(request);
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        private BitmapImage BitmapFromBytes(byte[] bytes)
        {
            if (!bytes.IsNullOrEmpty())
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = stream;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    return img;
                }
            }

            return null;
        }

        public static BitmapImage SetImageSize(ImageSource src, int width, int height)
        {
            if (src != null)
            {
                var encoder = new PngBitmapEncoder();
                var stream = new MemoryStream();
                var img = new BitmapImage();
                var bmp = src as BitmapSource;
                if (bmp != null)
                {
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(stream);

                    img.BeginInit();
                    if (width > 0) img.DecodePixelWidth = width;
                    if (height > 0) img.DecodePixelHeight = height;
                    img.StreamSource = new MemoryStream(stream.ToArray());
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();

                    stream.Close();

                    return img;
                }
            }

            return null;
        }
    }

    
}
