// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Requests;
using Data = TrakHound.Api.v2.Data;

namespace TrakHound.DeviceMonitor.Pages.Overview
{
    /// <summary>
    /// Interaction logic for OverviewPanel.xaml
    /// </summary>
    public partial class DevicePanel : UserControl, IComparable
    {
        private ManualResetEvent stop;
        private int interval = 10000;

        private Samples.Stream samplesStream;
        private Activity.Stream activityStream;
        private Alarms.Stream alarmStream;
        private Oee.Stream oeeStream;

        private List<string> dataItemIds = new List<string>();

        public Data.DeviceModel Model { get; set; }


        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register("Index", typeof(int), typeof(DevicePanel), new PropertyMetadata(-1));


        public delegate void IndexChanged_Handler(string deviceId, int index);
        public event IndexChanged_Handler IndexChanged;

        private string _deviceId;
        public string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }

        #region "Dependency Properties"

        private ObservableCollection<StatusPanel> _statusPanels;
        public ObservableCollection<StatusPanel> StatusPanels
        {
            get
            {
                if (_statusPanels == null) _statusPanels = new ObservableCollection<StatusPanel>();
                return _statusPanels;
            }
            set
            {
                _statusPanels = value;
            }
        }

        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(DevicePanel), new PropertyMetadata(true));


        public bool Connected
        {
            get { return (bool)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(DevicePanel), new PropertyMetadata(true));


        public bool ShowMoreInfo
        {
            get { return (bool)GetValue(ShowMoreInfoProperty); }
            set { SetValue(ShowMoreInfoProperty, value); }
        }

        public static readonly DependencyProperty ShowMoreInfoProperty =
            DependencyProperty.Register("ShowMoreInfo", typeof(bool), typeof(DevicePanel), new PropertyMetadata(false));


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
            DependencyProperty.Register("DeviceDescription", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// MTConnect Device Name
        /// </summary>
        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set { SetValue(DeviceNameProperty, value); }
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register("DeviceName", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// MTConnect Device ID
        /// </summary>
        public string ID
        {
            get { return (string)GetValue(IDProperty); }
            set { SetValue(IDProperty, value); }
        }

        public static readonly DependencyProperty IDProperty =
            DependencyProperty.Register("ID", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// Manufacturer of the Device
        /// </summary>
        public string DeviceManufacturer
        {
            get { return (string)GetValue(DeviceManufacturerProperty); }
            set { SetValue(DeviceManufacturerProperty, value); }
        }

        public static readonly DependencyProperty DeviceManufacturerProperty =
            DependencyProperty.Register("DeviceManufacturer", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// Model Number of the Device
        /// </summary>
        public string DeviceModel
        {
            get { return (string)GetValue(DeviceModelProperty); }
            set { SetValue(DeviceModelProperty, value); }
        }

        public static readonly DependencyProperty DeviceModelProperty =
            DependencyProperty.Register("DeviceModel", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// Serial Number of the Device
        /// </summary>
        public string DeviceSerial
        {
            get { return (string)GetValue(DeviceSerialProperty); }
            set { SetValue(DeviceSerialProperty, value); }
        }

        public static readonly DependencyProperty DeviceSerialProperty =
            DependencyProperty.Register("DeviceSerial", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));


        /// <summary>
        /// Connection Address
        /// </summary>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// Connection Port
        /// </summary>
        public int Port
        {
            get { return (int)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(int), typeof(DevicePanel), new PropertyMetadata(0));


        /// <summary>
        /// Image of the Device
        /// </summary>
        public ImageSource DeviceImage
        {
            get { return (ImageSource)GetValue(DeviceImageProperty); }
            set { SetValue(DeviceImageProperty, value); }
        }

        public static readonly DependencyProperty DeviceImageProperty =
            DependencyProperty.Register("DeviceImage", typeof(ImageSource), typeof(DevicePanel), new PropertyMetadata(null));

        /// <summary>
        /// Logo for the Device
        /// </summary>
        public ImageSource DeviceLogo
        {
            get { return (ImageSource)GetValue(DeviceLogoProperty); }
            set { SetValue(DeviceLogoProperty, value); }
        }

        public static readonly DependencyProperty DeviceLogoProperty =
            DependencyProperty.Register("DeviceLogo", typeof(ImageSource), typeof(DevicePanel), new PropertyMetadata(null));

        #endregion

        #endregion


        public DevicePanel(Data.DeviceModel model)
        {
            InitializeComponent();
            DataContext = this;

            _deviceId = model.DeviceId;
            Model = model;

            UpdateModel(model);
        }

        public void Start(DateTime from, DateTime to)
        {
            Loading = true;

            stop = new ManualResetEvent(false);

            // Download Images using the TrakHound Images Api
            if (!string.IsNullOrEmpty(Model.Manufacturer))
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
                if (!string.IsNullOrEmpty(Model.Model))
                {
                    var imageUrl = "https://images.trakhound.com/device-image?manufacturer=" + Model.Manufacturer + "&model=" + Model.Model;
                    var imageRequest = new Task<byte[]>(() => DownloadImage(imageUrl));
                    imageRequest.ContinueWith(imgRequest =>
                    {
                        if (imgRequest != null)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                var img = BitmapFromBytes(imgRequest.Result);
                                if (img != null)
                                {
                                    if (img.PixelHeight > 75 || img.PixelWidth > 75) DeviceImage = SetImageSize(img, 0, 75);
                                    else DeviceImage = img;
                                }
                            }), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
                        }
                    });
                    imageRequest.Start();
                }
            }


            var loopRequest = new Task(() =>
            {

                bool first = true;

                do
                {
                    var status = Status.Get(MainWindow._apiUrl, _deviceId, MainWindow._apiToken);
                    if (status != null)
                    {
                        UpdateStatus(status);

                        // Start Streams
                        if (status.Connected && activityStream == null) StartActivityStream();
                        if (status.Connected && alarmStream == null) StartAlarmStream();
                        if (status.Connected && samplesStream == null) StartSamplesStream();
                        if (status.Connected && oeeStream == null) StartOeeStream(from, to);
                    }

                    if (first) Dispatcher.BeginInvoke(new Action(() => { Loading = false; }));
                    first = false;

                } while (!stop.WaitOne(interval, true));

            }, TaskCreationOptions.LongRunning);
            loopRequest.Start();
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
            stop = null;

            if (samplesStream != null) samplesStream.Stop();
            samplesStream = null;

            if (activityStream != null) activityStream.Stop();
            activityStream = null;

            if (alarmStream != null) alarmStream.Stop();
            alarmStream = null;

            if (oeeStream != null) oeeStream.Stop();
            oeeStream = null;
        }

        public void UpdateTimespan(DateTime from, DateTime to)
        {
            if (oeeStream != null) oeeStream.Stop();

            if (Connected)
            {
                StartOeeStream(from, to);
            }
        }


        private void UpdateModel(Data.DeviceModel model)
        {
            if (model != null)
            {
                StatusPanels.Clear();

                if (model.Connection != null)
                {
                    Address = model.Connection.Address;
                    Port = model.Connection.Port;
                }

                ID = model.Id;
                DeviceName = model.Name;
                DeviceManufacturer = model.Manufacturer;
                DeviceModel = model.Model;
                DeviceSerial = model.SerialNumber;
                DeviceDescription = !string.IsNullOrEmpty(model.Description) ? model.Description.Trim() : null;


                var dataItems = model.GetDataItems();

                Data.DataItem execution = null;
                Data.DataItem controllerMode = null;

                bool multipleStatuses = false;

                // Find Availability
                var avail = model.GetDataItems().Find(o => o.Type == "AVAILABILITY");

                // Find Functional Mode
                var functionalMode = model.GetDataItems().Find(o => o.Type == "FUNCTIONAL_MODE");

                // Find Emergency Stop
                var estop = model.GetDataItems().Find(o => o.Type == "EMERGENCY_STOP");

                // Add Alarm Conditions
                dataItemIds.AddRange(model.GetDataItems().FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

                // Get Paths
                var paths = model.GetComponents().FindAll(o => o.Type == "Path");
                if (paths != null && paths.Count > 1)
                {
                    foreach (var path in paths)
                    {
                        if (path.DataItems != null && path.DataItems.Exists(o => o.Type == "EXECUTION"))
                        {
                            multipleStatuses = true;

                            // Create a new StatusPanel control
                            var statusPanel = new StatusPanel(DeviceId, path);
                            if (avail != null) statusPanel.AvailabilityId = avail.Id;
                            if (estop != null) statusPanel.EmergencyStopId = estop.Id;
                            dataItemIds.AddRange(statusPanel.GetIds());
                            StatusPanels.Add(statusPanel);
                        }
                    }
                }

                if (!multipleStatuses)
                {
                    execution = model.GetDataItems().Find(o => o.Type == "EXECUTION");
                    controllerMode = model.GetDataItems().Find(o => o.Type == "CONTROLLER_MODE");

                    if (execution != null || controllerMode != null)
                    {
                        var statusPanel = new StatusPanel();
                        statusPanel.DeviceId = DeviceId;

                        var obj = model.GetDataItems().Find(o => o.Type == "EXECUTION");
                        if (obj != null) statusPanel.ExecutionId = obj.Id;

                        obj = model.GetDataItems().Find(o => o.Type == "CONTROLLER_MODE");
                        if (obj != null) statusPanel.ControllerModeId = obj.Id;

                        // Get Alarm Condition IDs
                        statusPanel.AlarmIds.AddRange(model.GetDataItems().FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

                        // Get Program ID
                        obj = model.GetDataItems().Find(o => o.Type == "PROGRAM" && string.IsNullOrEmpty(o.SubType));
                        if (obj != null) statusPanel.ProgramNameId = obj.Id;

                        // Get Message
                        obj = model.GetDataItems().Find(o => o.Type == "MESSAGE");
                        if (obj != null) statusPanel.MessageId = obj.Id;

                        // Feedrate Override
                        obj = model.GetDataItems().Find(o => (o.Type == "PATH_FEEDRATE_OVERRIDE" && o.SubType == "PROGRAMMED") || (o.Type == "PATH_FEEDRATE" && o.Units == "PERCENT"));
                        if (obj != null) statusPanel.FeedrateOverrideId = obj.Id;

                        paths = model.GetComponents().FindAll(o => o.Type == "Path");
                        foreach (var path in paths)
                        {
                            statusPanel.PathPanels.Add(new PathPanel(path));
                        }

                        if (avail != null) statusPanel.AvailabilityId = avail.Id;
                        if (estop != null) statusPanel.EmergencyStopId = estop.Id;
                        dataItemIds.AddRange(statusPanel.GetIds());
                        StatusPanels.Add(statusPanel);
                    }
                }


                if (samplesStream != null) samplesStream.DataItemIds = dataItemIds.ToArray();
            }
        }

        public void UpdateStatus(Data.Status status)
        {
            if (status != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Connected = status.Connected;
                }));
            }
        }


        private void StartActivityStream()
        {
            activityStream = new Activity.Stream(MainWindow._apiUrl, _deviceId, 2000, MainWindow._apiToken);
            activityStream.ActivityReceived += Stream_ActivityReceived;
            activityStream.Start();
        }

        private void Stream_ActivityReceived(Data.ActivityItem activity)
        {
            if (activity.Events != null)
            {
                // Get Device Status
                var deviceStatus = activity.Events.Find(o => o.Name == "Status");
                if (deviceStatus != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var panel in StatusPanels)
                        {
                            panel.DeviceStatus = deviceStatus.Value;
                            panel.DeviceStatusTime = deviceStatus.Timestamp;
                            panel.DeviceStatusDescription = deviceStatus.ValueDescription;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background, null);
                }

                // Get Program Status
                var programStatus = activity.Events.Find(o => o.Name == "Program Status");
                if (programStatus != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var panel in StatusPanels)
                        {
                            panel.ProgramStatus = programStatus.Value;
                            panel.ProgramStatusTime = programStatus.Timestamp;
                            panel.ProgramStatusDescription = programStatus.ValueDescription;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background, null);
                }
            }
        }


        private void StartSamplesStream()
        {
            samplesStream = new Samples.Stream(MainWindow._apiUrl, _deviceId, 2000, dataItemIds.ToArray(), MainWindow._apiToken);
            samplesStream.SampleReceived += Stream_SampleReceived;
            samplesStream.Start();
        }

        private void Stream_SampleReceived(Data.Sample sample)
        {
            Dispatcher.BeginInvoke(new Action(() => {

                foreach (var statusPanel in StatusPanels) statusPanel.Update(sample);
            }));
        }


        private void StartAlarmStream()
        {
            alarmStream = new Alarms.Stream(MainWindow._apiUrl, _deviceId, 5000, MainWindow._apiToken);
            alarmStream.AlarmReceived += Stream_AlarmReceived;
            alarmStream.Start();
        }

        private void Stream_AlarmReceived(Data.Alarm alarm)
        {
            Dispatcher.BeginInvoke(new Action(() => {

                foreach (var statusPanel in StatusPanels)
                {
                    if (statusPanel.AlarmIds.Exists(o => o == alarm.DataItemId))
                    {
                        statusPanel.Update(alarm);
                    }
                }
            }));
        }


        private void StartOeeStream(DateTime from, DateTime to)
        {
            oeeStream = new Oee.Stream(MainWindow._apiUrl, _deviceId, from.ToUniversalTime(), to.ToUniversalTime(), 30000, 0, MainWindow._apiToken);
            oeeStream.OeeReceived += Stream_OeeReceived;
            oeeStream.Start();
        }

        private void Stream_OeeReceived(List<Data.Oee> oees)
        {
            if (oees != null && oees.Count > 0)
            {
                Dispatcher.BeginInvoke(new Action(() => {

                    var oee = oees[0];
                    foreach (var statusPanel in StatusPanels) statusPanel.Update(oee);
                }));
            }
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

        private void IndexUp_Clicked(TrakHound_UI.Button bt) { IndexChanged?.Invoke(DeviceId, Index - 1); }

        private void IndexDown_Clicked(TrakHound_UI.Button bt) { IndexChanged?.Invoke(DeviceId, Index + 1); }

        private void MoreInfo_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowMoreInfo = !ShowMoreInfo;
        }

        #region "IComparable"

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var i = obj as DevicePanel;
            if (i != null)
            {
                if (i > this) return -1;
                else if (i < this) return 1;
                else return 0;
            }
            else return 1;
        }


        #region "Private"

        static bool EqualTo(DevicePanel d1, DevicePanel d2)
        {
            if (!object.ReferenceEquals(d1, null) && object.ReferenceEquals(d2, null)) return false;
            if (object.ReferenceEquals(d1, null) && !object.ReferenceEquals(d2, null)) return false;
            if (object.ReferenceEquals(d1, null) && object.ReferenceEquals(d2, null)) return true;

            return d1.Index == d2.Index;
        }

        static bool NotEqualTo(DevicePanel d1, DevicePanel d2)
        {
            if (!object.ReferenceEquals(d1, null) && object.ReferenceEquals(d2, null)) return true;
            if (object.ReferenceEquals(d1, null) && !object.ReferenceEquals(d2, null)) return true;
            if (object.ReferenceEquals(d1, null) && object.ReferenceEquals(d2, null)) return false;

            return d1.Index != d2.Index;
        }

        static bool LessThan(DevicePanel d1, DevicePanel d2)
        {
            if (d1.Index > d2.Index) return false;
            else return true;
        }

        static bool GreaterThan(DevicePanel d1, DevicePanel d2)
        {
            if (d1.Index < d2.Index) return false;
            else return true;
        }

        #endregion

        public static bool operator ==(DevicePanel d1, DevicePanel d2)
        {
            return EqualTo(d1, d2);
        }

        public static bool operator !=(DevicePanel d1, DevicePanel d2)
        {
            return NotEqualTo(d1, d2);
        }


        public static bool operator <(DevicePanel d1, DevicePanel d2)
        {
            return LessThan(d1, d2);
        }

        public static bool operator >(DevicePanel d1, DevicePanel d2)
        {
            return GreaterThan(d1, d2);
        }


        public static bool operator <=(DevicePanel d1, DevicePanel d2)
        {
            return LessThan(d1, d2) || EqualTo(d1, d2);
        }

        public static bool operator >=(DevicePanel d1, DevicePanel d2)
        {
            return GreaterThan(d1, d2) || EqualTo(d1, d2);
        }

        #endregion

    }
}
