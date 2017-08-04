// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Requests;
using Json = TrakHound.Api.v2.Json;

namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DEFAULT_API_URL = "http://localhost:8478/";

        private static Logger log = LogManager.GetCurrentClassLogger();

        private System.Timers.Timer timespanUpdateTimer;
        private System.Timers.Timer addDevicesUpdateTimer;

        private List<TempServer.MTConnect.MTConnectConnection> addDeviceQueue = new List<TempServer.MTConnect.MTConnectConnection>();

        public static string _apiUrl = DEFAULT_API_URL;
        public static string _apiToken = null;

        #region "Dependency Properties"

        private ObservableCollection<DeviceListItem> _deviceListItems;
        public ObservableCollection<DeviceListItem> DeviceListItems
        {
            get
            {
                if (_deviceListItems == null) _deviceListItems = new ObservableCollection<DeviceListItem>();
                return _deviceListItems;
            }
            set
            {
                _deviceListItems = value;
            }
        }

        private ObservableCollection<TempServer.MTConnect.MTConnectConnection> _mtconnectDeviceItems;
        public ObservableCollection<TempServer.MTConnect.MTConnectConnection> MTConnectDeviceItems
        {
            get
            {
                if (_mtconnectDeviceItems == null) _mtconnectDeviceItems = new ObservableCollection<TempServer.MTConnect.MTConnectConnection>();
                return _mtconnectDeviceItems;
            }
            set
            {
                _mtconnectDeviceItems = value;
            }
        }

        private ObservableCollection<DateTime> _toDateTimes;
        public ObservableCollection<DateTime> ToDateTimes
        {
            get
            {
                if (_toDateTimes == null)
                    _toDateTimes = new ObservableCollection<DateTime>();
                return _toDateTimes;
            }

            set
            {
                _toDateTimes = value;
            }
        }

        private ObservableCollection<DateTime> _fromDateTimes;
        public ObservableCollection<DateTime> FromDateTimes
        {
            get
            {
                if (_fromDateTimes == null)
                    _fromDateTimes = new ObservableCollection<DateTime>();
                return _fromDateTimes;
            }

            set
            {
                _fromDateTimes = value;
            }
        }


        public bool IsMenuShown
        {
            get { return (bool)GetValue(IsMenuShownProperty); }
            set { SetValue(IsMenuShownProperty, value); }
        }

        public static readonly DependencyProperty IsMenuShownProperty =
            DependencyProperty.Register("IsMenuShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public bool IsLargeMenuShown
        {
            get { return (bool)GetValue(IsLargeMenuShownProperty); }
            set { SetValue(IsLargeMenuShownProperty, value); }
        }

        public static readonly DependencyProperty IsLargeMenuShownProperty =
            DependencyProperty.Register("IsLargeMenuShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public bool IsAboutShown
        {
            get { return (bool)GetValue(IsAboutShownProperty); }
            set { SetValue(IsAboutShownProperty, value); }
        }

        public static readonly DependencyProperty IsAboutShownProperty =
            DependencyProperty.Register("IsAboutShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsOptionsShown
        {
            get { return (bool)GetValue(IsOptionsShownProperty); }
            set { SetValue(IsOptionsShownProperty, value); }
        }

        public static readonly DependencyProperty IsOptionsShownProperty =
            DependencyProperty.Register("IsOptionsShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsFindDevicesShown
        {
            get { return (bool)GetValue(IsFindDevicesShownProperty); }
            set { SetValue(IsFindDevicesShownProperty, value); }
        }

        public static readonly DependencyProperty IsFindDevicesShownProperty =
            DependencyProperty.Register("IsFindDevicesShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public double ZoomLevel
        {
            get { return (double)GetValue(ZoomLevelProperty); }
            set { SetValue(ZoomLevelProperty, value); }
        }

        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register("ZoomLevel", typeof(double), typeof(MainWindow), new PropertyMetadata(1d));


        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public bool FindDevicesLoading
        {
            get { return (bool)GetValue(FindDevicesLoadingProperty); }
            set { SetValue(FindDevicesLoadingProperty, value); }
        }

        public static readonly DependencyProperty FindDevicesLoadingProperty =
            DependencyProperty.Register("FindDevicesLoading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public DateTime FromTime
        {
            get { return (DateTime)GetValue(FromTimeProperty); }
            set { SetValue(FromTimeProperty, value); }
        }

        public static readonly DependencyProperty FromTimeProperty =
            DependencyProperty.Register("FromTime", typeof(DateTime), typeof(MainWindow), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(FromTime_PropertyChanged)));

        private static void FromTime_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as MainWindow;
            o.UpdateToTimes();
            o.UpdateTimespan();
        }

        public DateTime ToTime
        {
            get { return (DateTime)GetValue(ToTimeProperty); }
            set { SetValue(ToTimeProperty, value); }
        }

        public static readonly DependencyProperty ToTimeProperty =
            DependencyProperty.Register("ToTime", typeof(DateTime), typeof(MainWindow), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(ToTime_PropertyChanged)));

        private static void ToTime_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as MainWindow;
            o.UpdateTimespan();
        }


        public string AddDeviceAddress
        {
            get { return (string)GetValue(AddDeviceAddressProperty); }
            set { SetValue(AddDeviceAddressProperty, value); }
        }

        public static readonly DependencyProperty AddDeviceAddressProperty =
            DependencyProperty.Register("AddDeviceAddress", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int AddDevicePort
        {
            get { return (int)GetValue(AddDevicePortProperty); }
            set { SetValue(AddDevicePortProperty, value); }
        }

        public static readonly DependencyProperty AddDevicePortProperty =
            DependencyProperty.Register("AddDevicePort", typeof(int), typeof(MainWindow), new PropertyMetadata(5000));


        public string AddDeviceName
        {
            get { return (string)GetValue(AddDeviceNameProperty); }
            set { SetValue(AddDeviceNameProperty, value); }
        }

        public static readonly DependencyProperty AddDeviceNameProperty =
            DependencyProperty.Register("AddDeviceName", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            ServicePointManager.DefaultConnectionLimit = 1000;
            ThreadPool.SetMinThreads(100, 4);

            LoadTimespan();
            UpdateTimespan();

            // Load the Api Url
            _apiUrl = DEFAULT_API_URL;

            UpdateAppVersion();
            LoadDevices();

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            overviewPage.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            overviewPage.Stop();
        }

        private void LoadDevices()
        {
            Loading = true;

            // Clear Lists
            DeviceListItems.Clear();
            overviewPage.ClearDevices();

            // Get the Connections URL to use
            var url = _apiUrl;

            // Get a list of all Connections available from TrakHound Api
            var connectionsRequest = new Task<List<ConnectionDefinition>>(() => Connections.Get(url, _apiToken).ToList());
            connectionsRequest.ContinueWith(x =>
            {
                var connections = x.Result;
                if (!connections.IsNullOrEmpty())
                {
                    // Get Saved Device Configurations
                    var deviceConfigurations = Properties.Settings.Default.DeviceList;
                    bool addAll = deviceConfigurations.IsNullOrEmpty();

                    int newCount = 0;
                    int maxNewDevices = 5;
                    int newIndex = 0;

                    // Add each Device to SavedDeviceList
                    foreach (var connection in x.Result)
                    {
                        // Get the Device Model from TrakHound Api
                        var modelRequest = new Task<DeviceModel>(() => Model.Get(_apiUrl, connection.DeviceId, _apiToken));
                        modelRequest.ContinueWith(y =>
                        {
                            var model = y.Result;
                            if (model != null)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    var listItem = new DeviceListItem(model);
                                    int index = -1;

                                    var config = deviceConfigurations.Find(o => o.DeviceId == model.DeviceId);
                                    if (config != null)
                                    {
                                        listItem.Enabled = config.Enabled;
                                        listItem.PerformanceEnabled = config.PerformanceEnabled;
                                        listItem.QualityEnabled = config.QualityEnabled;
                                        listItem.Configuration = config;
                                        index = config.Index;
                                    }
                                    else
                                    {
                                        if (newCount < maxNewDevices)
                                        {
                                            config = new DeviceConfiguration();
                                            config.DeviceId = model.DeviceId;
                                            config.Enabled = true;
                                            config.Index = newIndex;
                                            index = newIndex;

                                            listItem.Enabled = true;
                                            listItem.PerformanceEnabled = true;
                                            listItem.QualityEnabled = false;
                                            listItem.Configuration = config;
                                        }

                                        newCount++;
                                        newIndex++;
                                    }

                                    listItem.CheckedChanged += DeviceListItem_CheckedChanged;
                                    DeviceListItems.Add(listItem);

                                    if (!IsOptionsShown && newCount > maxNewDevices) OpenOptionsPage();

                                    if (listItem.Enabled)
                                    {
                                        overviewPage.AddDevice(model, index);
                                    }

                                }), System.Windows.Threading.DispatcherPriority.Background, null);
                            }
                        });
                        modelRequest.Start();

                        Thread.Sleep(100);
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        overviewPage.SortDevices();
                        SaveDeviceList();
                        Loading = false;

                    }), System.Windows.Threading.DispatcherPriority.Background, null);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Loading = false;
                        OpenOptionsPage();

                    }), System.Windows.Threading.DispatcherPriority.Background, null);
                }
            });
            connectionsRequest.Start();
        }

        #region "Timespan"

        private void LoadTimespan()
        {
            // Load Current Day Start and Day End
            var d = DateTime.Now;
            var dayStart = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local);
            var dayEnd = dayStart.AddDays(1);

            FromDateTimes.Clear();
            for (var x = 0; x <= 24; x++) FromDateTimes.Add(dayStart.AddHours(x));

            // Load From Setting
            string fromSetting = Properties.Settings.Default.FromTime;
            DateTime savedFrom = DateTime.MinValue;
            DateTime.TryParse(fromSetting, out savedFrom);

            // Load To Setting
            string toSetting = Properties.Settings.Default.ToTime;
            DateTime savedTo = DateTime.MinValue;
            DateTime.TryParse(toSetting, out savedTo);

            // Set From
            if (savedFrom > dayStart) FromTime = savedFrom;
            else if (savedFrom > DateTime.MinValue) FromTime = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day, savedFrom.Hour, 0, 0, DateTimeKind.Local);
            else FromTime = dayStart;

            // Load ToDateTimes
            UpdateToTimes();

            // Set To
            if (savedTo > dayStart && savedTo < dayEnd && savedTo > FromTime) ToTime = savedTo;
            else if (savedTo > DateTime.MinValue && savedTo.Hour > 0) ToTime = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day, savedTo.Hour, 0, 0, DateTimeKind.Local);
            else ToTime = dayEnd;

            // Start/Restart TimespanUpdateTimer
            if (timespanUpdateTimer != null) timespanUpdateTimer.Stop();
            else
            {
                timespanUpdateTimer = new System.Timers.Timer();
                timespanUpdateTimer.Interval = 5000;
                timespanUpdateTimer.Elapsed += TimespanUpdateTimer_Elapsed;
            }

            timespanUpdateTimer.Start();
        }

        private void UpdateToTimes()
        {
            ToDateTimes.Clear();
            int i = 1;
            for (var x = FromTime.Hour; x <= (FromTime.Hour + 24); x++) ToDateTimes.Add(FromTime.AddHours(i++));
            if (ToTime <= FromTime) ToTime = FromTime.AddHours(1);
        }

        private void UpdateTimespan()
        {
            Properties.Settings.Default.FromTime = FromTime.ToString("o");
            Properties.Settings.Default.ToTime = ToTime.ToString("o");
            Properties.Settings.Default.Save();

            overviewPage.UpdateTimespan(FromTime, ToTime);
        }

        private void TimespanUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Load Current Day Start and Day End
                    var d = DateTime.Now;
                    var dayStart = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local);
                    var dayEnd = dayStart.AddDays(1);

                    DateTime from = DateTime.MinValue;
                    DateTime to = DateTime.MinValue;

                    // Set From
                    if (FromTime > dayStart) from = FromTime;
                    else if (FromTime > DateTime.MinValue) from = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day, FromTime.Hour, 0, 0);
                    else from = dayStart;

                    // Set To
                    if (ToTime > dayStart && ToTime <= dayEnd) to = ToTime;
                    else if (ToTime > DateTime.MinValue && ToTime.Hour > 0) to = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day, ToTime.Hour, 0, 0);
                    else to = dayEnd;

                    // Update Timespan
                    if (FromTime != from || ToTime != to)
                    {
                        FromDateTimes.Clear();
                        for (var x = 0; x <= 24; x++) FromDateTimes.Add(dayStart.AddHours(x));

                        FromTime = from;

                        // Load ToDateTimes
                        UpdateToTimes();

                        ToTime = to;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }

            }), System.Windows.Threading.DispatcherPriority.Background, new object[] { });
        }

        #endregion

        #region "Menu Bar"

        private void Refresh_Click(object sender, RoutedEventArgs e) { LoadDevices(); }

        private void Exit_Click(object sender, RoutedEventArgs e) { Close(); }


        private void About_Click(object sender, RoutedEventArgs e) { OpenAboutPage(); }

        private void Options_Click(object sender, RoutedEventArgs e) { OpenOptionsPage(); }

        private void FindDevices_Click(object sender, RoutedEventArgs e)
        {
            OpenFindDevicesPage();
            SearchForDevices();
        }

        #endregion

        #region "Toolbars"

        private void Refresh_Clicked(TrakHound_UI.Button bt) { LoadDevices(); }

        private void Options_Clicked(TrakHound_UI.Button bt) { OpenOptionsPage(); }

        private void FindDevices_Clicked(TrakHound_UI.Button bt)
        {
            OpenFindDevicesPage();
            SearchForDevices();
        }

        private void Back_Clicked(TrakHound_UI.Button bt) { HideMenu(); }

        private void Dock_Clicked(TrakHound_UI.Button bt) { }

        private void AutoFit_Clicked(TrakHound_UI.Button bt) { }

        #endregion

        #region "Menu"

        private void OpenAboutPage()
        {
            if (IsMenuShown && IsAboutShown) HideMenu();
            else
            {
                IsMenuShown = true;
                IsLargeMenuShown = false;
                HidePages();
                IsAboutShown = true;
            }
        }

        private void OpenOptionsPage()
        {
            if (IsMenuShown && IsOptionsShown) HideMenu();
            else
            {
                IsMenuShown = true;
                IsLargeMenuShown = true;
                HidePages();
                IsOptionsShown = true;
            }
        }

        private void OpenFindDevicesPage()
        {
            if (IsMenuShown && IsFindDevicesShown) HideMenu();
            else
            {
                IsMenuShown = true;
                IsLargeMenuShown = true;
                HidePages();
                IsFindDevicesShown = true;
            }
        }

        private void HideMenu()
        {
            IsMenuShown = false;
            IsLargeMenuShown = false;
            HidePages();
        }

        private void HidePages()
        {
            IsAboutShown = false;
            IsOptionsShown = false;
            IsFindDevicesShown = false;
        }

        private void ShadedPanel_MouseDown(object sender, MouseButtonEventArgs e) { HideMenu(); }

        
        #region "Options Menu"

        private void Docs_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://www.trakhound.com/docs");
        }

        #endregion

        #region "About Menu"

        private void License_Clicked(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.ToString());
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        #endregion

        #endregion

        #region "Device List"

        private void DeviceListItem_CheckedChanged(DeviceListItem item, bool value)
        {
            var deviceId = item.DeviceId;

            // Read savedDeviceList from Application Settings
            var savedDeviceList = Properties.Settings.Default.DeviceList;
            if (savedDeviceList != null)
            {
                // Find Item in saved DeviceList
                var config = savedDeviceList.Find(o => o.DeviceId == deviceId);
                if (config != null)
                {
                    if (value)
                    {
                        // Enabled DeviceListItem
                        config.Enabled = true;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            overviewPage.AddDevice(item.Model, config.Index);
                        }), System.Windows.Threading.DispatcherPriority.Background, null);
                    }
                    else
                    {
                        // Disabled DeviceListItem
                        config.Enabled = false;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            overviewPage.RemoveDevice(deviceId);
                        }), System.Windows.Threading.DispatcherPriority.Background, null);
                    }
                }

                SaveDeviceList();
            }
        }

        private void DeviceList_EnableAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var item in DeviceListItems)
            {
                item.SuppressCheckedChanged = true;
                item.Enabled = true;
                item.SuppressCheckedChanged = false;

                var config = Properties.Settings.Default.DeviceList.Find(o => o.DeviceId == item.DeviceId);
                if (config != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        overviewPage.AddDevice(item.Model, config.Index);
                    }), System.Windows.Threading.DispatcherPriority.Background, null);
                } 
            }

            SaveDeviceList();
        }

        private void DeviceList_DisableAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var item in DeviceListItems)
            {
                item.SuppressCheckedChanged = true;
                item.Enabled = false;
                item.SuppressCheckedChanged = false;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    overviewPage.RemoveDevice(item.DeviceId);
                }), System.Windows.Threading.DispatcherPriority.Background, null);
            }

            SaveDeviceList();
        }

        private void SaveDeviceList()
        {
            var configList = new List<DeviceConfiguration>();

            foreach (var listItem in DeviceListItems)
            {
                configList.Add(listItem.Configuration);
            }

            Properties.Settings.Default.DeviceList = configList;
            Properties.Settings.Default.Save();
        }

        private void OpenConfigurator_Clicked(TrakHound_UI.Button bt) { OpenDeviceConfigurator(); }

        #endregion

        #region "Zoom"

        private void ZoomIn_Clicked(TrakHound_UI.Button bt)
        {
            ZoomLevel = Math.Min(5, ZoomLevel + 0.05);
        }

        private void ZoomOut_Clicked(TrakHound_UI.Button bt)
        {
            ZoomLevel = Math.Max(0.5, ZoomLevel - 0.05);
        }

        private void ZoomReset_Clicked(TrakHound_UI.Button bt)
        {
            ZoomLevel = 1;
        }

        #endregion

        #region "Program Access"

        private void OpenDeviceConfigurator()
        {
            var configuratorFilename = "TrakHound-DataClient-Configurator.exe";

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (Directory.Exists(programFiles))
            {
                var filename = Path.Combine(programFiles, "TrakHound", "DataClient", configuratorFilename);
                if (File.Exists(filename)) Process.Start(filename);
            }
            else
            {
                var filename = Path.Combine(programFiles, "TrakHound", "DataClient", configuratorFilename);
                if (File.Exists(filename)) Process.Start(filename);
            }
        }

        #endregion
        

        private static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            bool upper = false;

            foreach (var c in s)
            {
                if (char.IsUpper(c))
                {
                    upper = true;
                    break;
                }
            }

            if (!upper) return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
            return s;
        }

        private void UpdateAppVersion()
        {
            // Update Application Settings
            var previousVersion = Properties.Settings.Default.AppVersion;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.ToString() != previousVersion)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.AppVersion = version.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void ResetFromTime_Clicked(TrakHound_UI.Button bt)
        {
            var d = DateTime.Now;
            FromTime = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local);
        }

        private void ResetToTime_Clicked(TrakHound_UI.Button bt)
        {
            var d = DateTime.Now;
            ToTime = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local).AddDays(1);
        }

        private void Page_IndexChanged(string deviceId, int index)
        {
            var configs = Properties.Settings.Default.DeviceList;
            if (configs != null)
            {
                var modifiedConfig = configs.Find(o => o.DeviceId == deviceId);
                if (modifiedConfig != null)
                {
                    int oldIndex = modifiedConfig.Index;
                    int newIndex = index;

                    if (oldIndex < index) // Index Down
                    {
                        var configNext = configs.Find(o => o.Index == oldIndex + 1);
                        if (configNext != null)
                        {
                            configNext.Index = index - 1;
                            overviewPage.UpdateDeviceIndex(configNext.DeviceId, configNext.Index);
                        }
                    }
                    else // Index Up
                    {
                        var configBefore = configs.Find(o => o.Index == oldIndex - 1);
                        if (configBefore != null)
                        {
                            configBefore.Index = index + 1;
                            overviewPage.UpdateDeviceIndex(configBefore.DeviceId, configBefore.Index);
                        }
                    }

                    modifiedConfig.Index = Math.Max(0, Math.Min(index, configs.FindAll(o => o.Enabled).Count - 1));
                    overviewPage.UpdateDeviceIndex(modifiedConfig.DeviceId, modifiedConfig.Index);
                }
            }

            overviewPage.SortDevices();
            Properties.Settings.Default.Save();
        }

        private void SearchForDevices_Clicked(TrakHound_UI.Button bt)
        {
            SearchForDevices();
        }

        private void SearchForDevices()
        {
            FindDevicesLoading = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                var baseUrl = "http://localhost:8479/";

                var client = new RestClient(baseUrl);
                var request = new RestRequest("devices", Method.GET);

                var deviceItems = new List<TempServer.MTConnect.MTConnectConnection>();

                var response = client.Execute(request);
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    var json = response.Content;
                    if (!string.IsNullOrEmpty(json))
                    {
                        var obj = Json.Convert.FromJson<List<TrakHound.TempServer.MTConnect.MTConnectConnection>>(json);
                        if (obj != null) deviceItems.AddRange(obj);
                    }
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MTConnectDeviceItems.Clear();

                    foreach (var deviceItem in deviceItems)
                    {
                        if (!DeviceListItems.ToList().Exists(x => x.DeviceId == deviceItem.DeviceId))
                        {
                            MTConnectDeviceItems.Add(deviceItem);
                        }
                    }

                    FindDevicesLoading = false;
                }));
            }));
        }

        private void FindDevicesAdd_Clicked(TrakHound_UI.Button bt)
        {
            var connection = (TempServer.MTConnect.MTConnectConnection)bt.DataObject;

            AddMTConnectConnection(connection);

            var i = MTConnectDeviceItems.ToList().FindIndex(o => o.DeviceId == connection.DeviceId);
            if (i >= 0) MTConnectDeviceItems.RemoveAt(i);
        }

        private void FindDevicesAddAll_Clicked(TrakHound_UI.Button bt)
        {
            AddDevicesToServer(MTConnectDeviceItems.ToList());
        }

        private void AddMTConnectConnection(TempServer.MTConnect.MTConnectConnection connection)
        {
            addDeviceQueue.Add(connection);

            if (addDevicesUpdateTimer != null) addDevicesUpdateTimer.Stop();

            addDevicesUpdateTimer = new System.Timers.Timer();
            addDevicesUpdateTimer.Interval = 2000;
            addDevicesUpdateTimer.Elapsed += AddDevicesUpdateTimer_Elapsed;
            addDevicesUpdateTimer.Start();
        }

        private void AddDevicesUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var timer = (System.Timers.Timer)sender;
            timer.Stop();

            AddDevicesToServer(addDeviceQueue.ToList());
            addDeviceQueue.Clear();
        }

        private void AddDevicesToServer(List<TempServer.MTConnect.MTConnectConnection> devices)
        {
            if (!devices.IsNullOrEmpty())
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
                {
                    var baseUrl = "http://localhost:8479/";
                    var client = new RestClient(baseUrl);
                    var request = new RestRequest(Method.GET);
                    var response = client.Execute(request);
                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        var json = response.Content;
                        if (!string.IsNullOrEmpty(json))
                        {
                            var config = Json.Convert.FromJson<TempServer.Configuration>(json);
                            if (config != null)
                            {
                                foreach (var device in devices)
                                {
                                    config.Devices.Add(device);
                                }

                                request = new RestRequest(Method.POST);
                                json = Json.Convert.ToJson(config);
                                request.AddParameter("text/xml", json, ParameterType.RequestBody);
                                response = client.Execute(request);
                                if (response == null || response.StatusCode != HttpStatusCode.OK)
                                {


                                }
                            }
                        }
                    }
                }));
            }
        }

        private void AddDevice_Clicked(TrakHound_UI.Button bt)
        {
            var connection = new TempServer.MTConnect.MTConnectConnection();
            connection.DeviceId = Guid.NewGuid().ToString();
            connection.Address = AddDeviceAddress;
            connection.Port = AddDevicePort;
            connection.DeviceName = AddDeviceName;

            AddMTConnectConnection(connection);
        }
    }
}
