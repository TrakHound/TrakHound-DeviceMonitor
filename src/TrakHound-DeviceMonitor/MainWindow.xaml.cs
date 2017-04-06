// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using TrakHound.Api.v2.Authentication;
using TrakHound.Api.v2.Requests;
using System.Linq;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private System.Timers.Timer tokenRefreshTimer;

        public static string _apiUrl = "http://localhost";
        public static string _apiToken = null;

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

        #region "Dependency Properties"

        public bool MenuShown
        {
            get { return (bool)GetValue(MenuShownProperty); }
            set { SetValue(MenuShownProperty, value); }
        }

        public static readonly DependencyProperty MenuShownProperty =
            DependencyProperty.Register("MenuShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool AboutShown
        {
            get { return (bool)GetValue(AboutShownProperty); }
            set { SetValue(AboutShownProperty, value); }
        }

        public static readonly DependencyProperty AboutShownProperty =
            DependencyProperty.Register("AboutShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool LoginShown
        {
            get { return (bool)GetValue(LoginShownProperty); }
            set { SetValue(LoginShownProperty, value); }
        }

        public static readonly DependencyProperty LoginShownProperty =
            DependencyProperty.Register("LoginShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool OptionsShown
        {
            get { return (bool)GetValue(OptionsShownProperty); }
            set { SetValue(OptionsShownProperty, value); }
        }

        public static readonly DependencyProperty OptionsShownProperty =
            DependencyProperty.Register("OptionsShown", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public bool LoggedIn
        {
            get { return (bool)GetValue(LoggedInProperty); }
            set { SetValue(LoggedInProperty, value); }
        }

        public static readonly DependencyProperty LoggedInProperty =
            DependencyProperty.Register("LoggedIn", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        public static readonly DependencyProperty UsernameProperty =
            DependencyProperty.Register("Username", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

        public string ApiUrl
        {
            get { return (string)GetValue(ApiUrlProperty); }
            set { SetValue(ApiUrlProperty, value); }
        }

        public static readonly DependencyProperty ApiUrlProperty =
            DependencyProperty.Register("ApiUrl", typeof(string), typeof(MainWindow), new PropertyMetadata(null, new PropertyChangedCallback(ApiUrl_PropertyChanged)));

        private static void ApiUrl_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            _apiUrl = (string)e.NewValue;
        }


        public int ApiInterval
        {
            get { return (int)GetValue(ApiIntervalProperty); }
            set { SetValue(ApiIntervalProperty, value); }
        }

        public static readonly DependencyProperty ApiIntervalProperty =
            DependencyProperty.Register("ApiInterval", typeof(int), typeof(MainWindow), new PropertyMetadata(500));

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            UpdateAppVersion();
            LoadWindow();
            LoadUser();

            ApiUrl = _apiUrl;
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void LoadDevices()
        {
            Loading = true;

            // Stop Devices in List
            foreach (var item in DeviceItems) item.Stop();

            // Clear List
            DeviceItems.Clear();
            DeviceListItems.Clear();
            var username = Username;

            // Get Devices from Api
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var savedDeviceList = Properties.Settings.Default.DeviceList;
                bool addAll = savedDeviceList == null;
                var saveDeviceList = new List<string>();

                var url = _apiUrl;
                if (!string.IsNullOrEmpty(username))
                {
                    var uri = new Uri(url);
                    url = new Uri(uri, username).ToString();
                }

                var connections = Connections.Get(url, _apiToken);
                foreach (var connection in connections)
                {
                    bool add = addAll || savedDeviceList.Exists(x => x == connection.DeviceId);
                    if (add) saveDeviceList.Add(connection.DeviceId);

                    // Add to Options Device List
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Add to Options Device List
                        var deviceListItem = new DeviceListItem(connection);
                        deviceListItem.CheckedChanged += DeviceListItem_CheckedChanged;
                        deviceListItem.SuppressCheckedChanged = true;
                        deviceListItem.Checked = add;
                        deviceListItem.SuppressCheckedChanged = false;
                        DeviceListItems.Add(deviceListItem);
                    }));

                    // Add to Monitored Device List
                    if (add)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var deviceItem = new DeviceItem(connection);
                            deviceItem.Start();
                            DeviceItems.Add(deviceItem);
                        }));
                    }
                }

                SaveDeviceList(saveDeviceList);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Loading = false;
                    if (DeviceItems.Count == 0 && !MenuShown) OpenOptionsPage();
                }));
            });
        }

        private void SaveDeviceList(List<string> list)
        {
            Properties.Settings.Default.DeviceList = list;
            Properties.Settings.Default.Save();
        }

        private void DeviceListItem_CheckedChanged(DeviceListItem item, bool value)
        {
            if (item.Connection != null)
            {
                var deviceId = item.DeviceId;

                var savedDeviceList = Properties.Settings.Default.DeviceList;

                if (value)
                {
                    // Add to SavedList
                    if (savedDeviceList == null)
                    {
                        savedDeviceList = new List<string>();
                        savedDeviceList.Add(deviceId);
                    }
                    else
                    {
                        if (!savedDeviceList.Exists(o => o == deviceId)) savedDeviceList.Add(deviceId);
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Add to DeviceList
                        var deviceItem = new DeviceItem(item.Connection);
                        deviceItem.Start();
                        DeviceItems.Add(deviceItem);
                    }));
                }
                else
                {
                    // Remove from SavedList
                    if (savedDeviceList != null)
                    {
                        int i = savedDeviceList.FindIndex(o => o == deviceId);
                        if (i >= 0) savedDeviceList.RemoveAt(i);
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Remove from DeviceList
                        int j = DeviceItems.ToList().FindIndex(o => o.DeviceId == deviceId);
                        if (j >= 0)
                        {
                            DeviceItems[j].Stop();
                            DeviceItems.RemoveAt(j);
                        }
                    }));
                }

                SaveDeviceList(savedDeviceList);
            }
        }

        private void DockRight()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
         
            Width = 350;
            Height = screenHeight - 50;

            Top = 0;
            Left = screenWidth - Width;
        }

        private void AutoFitWindow()
        {
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            double itemHeights = 0;
            foreach (var item in DeviceItems) itemHeights += item.ActualHeight;

            Height = Math.Max(Math.Min(screenHeight - 50, itemHeights + 90), 150);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var deviceItem in DeviceItems) deviceItem.Stop();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { SaveWindow(); }

        private void Window_LocationChanged(object sender, EventArgs e) { SaveWindow(); }

        private void Signup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start("https://www.trakhound.com/join");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        private void Refresh_Click(object sender, RoutedEventArgs e) { LoadDevices(); }

        private void Exit_Click(object sender, RoutedEventArgs e) { Close(); }

        private void About_Click(object sender, RoutedEventArgs e) { OpenAboutPage(); }

        private void Options_Click(object sender, RoutedEventArgs e) { OpenOptionsPage(); }

        private void Dock_Click(object sender, RoutedEventArgs e) { DockRight(); }

        private void AutoFit_Click(object sender, RoutedEventArgs e) { AutoFitWindow(); }

        private void Refresh_Clicked(TrakHound_UI.Button bt) { LoadDevices(); }

        private void LoginMenu_Clicked(TrakHound_UI.Button bt) { OpenLoginPage(); }

        private void Options_Clicked(TrakHound_UI.Button bt) { OpenOptionsPage(); }

        private void Back_Clicked(TrakHound_UI.Button bt) { HideMenu(); }

        private void Dock_Clicked(TrakHound_UI.Button bt) { DockRight(); }

        private void AutoFit_Clicked(TrakHound_UI.Button bt) { AutoFitWindow(); }

        private void Login_Clicked(TrakHound_UI.Button bt)
        {
            Login();
            LoadDevices();
        }

        private void Logout_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Logout();
            LoadDevices();
        }

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


        private void OpenAboutPage()
        {
            if (MenuShown && AboutShown) HideMenu();
            else
            {
                MenuShown = true;
                HidePages();
                AboutShown = true;
            }
        }

        private void OpenLoginPage()
        {
            if (MenuShown && LoginShown) HideMenu();
            else
            {
                MenuShown = true;
                HidePages();
                if (!LoggedIn) LoginShown = true;
                else OptionsShown = true;
            }
        }

        private void OpenOptionsPage()
        {
            if (MenuShown && OptionsShown) HideMenu();
            else
            {
                MenuShown = true;
                HidePages();
                OptionsShown = true;
            }
        }

        private void HideMenu()
        {
            MenuShown = false;
            HidePages();
        }

        private void HidePages()
        {
            AboutShown = false;
            LoginShown = false;
            OptionsShown = false;        
        }


        #region "User"

        private void Login()
        {
            Login(Username, Password);
            Password = null;
        }

        private void Login(string username, string password)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var token = Token.Create(username, password);
                password = null;
                Login(token);
            });
        }

        private void Login(string tokenId)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var token = Token.Get(tokenId);
                Login(token);
            });
        }

        private void Login(Token token)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (token != null)
                {
                    LoggedIn = true;
                    _apiToken = token.Id;
                    ApiUrl = "https://api.trakhound.com/data/";

                    var user = User.Get(token.Id);
                    if (user != null)
                    {
                        Username = UppercaseFirst(user.Username);
                    }

                    SaveUser();
                    StartTokenRefreshTimer();
                }
                else
                {
                    LoggedIn = false;
                    OpenLoginPage();
                    StopTokenRefreshTimer();
                }

                LoadDevices();
            }));
        }

        private void Logout()
        {
            if (_apiToken != null) Token.Delete(_apiToken);

            Username = null;
            LoggedIn = false;

            DeleteUser();
            StopTokenRefreshTimer();
        }

        private void StartTokenRefreshTimer()
        {
            tokenRefreshTimer = new System.Timers.Timer();
            tokenRefreshTimer.Interval = 5 * 60 * 1000; // 5 Minutes
            tokenRefreshTimer.Elapsed += TokenRefreshTimer_Elapsed;
            tokenRefreshTimer.Start();
        }

        private void StopTokenRefreshTimer()
        {
            if (tokenRefreshTimer != null)
            {
                tokenRefreshTimer.Stop();
                tokenRefreshTimer.Dispose();
            }
        }

        private void TokenRefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var token = Token.Get(_apiToken);
            if (token == null)
            {
                Logout();
                OpenLoginPage();
            }
        }

        #endregion


        private void DeviceList_CheckAll_Clicked(TrakHound_UI.Button bt)
        {
            var saveList = new List<string>();

            foreach (var item in DeviceListItems)
            {
                item.SuppressCheckedChanged = true;
                item.Checked = true;
                item.SuppressCheckedChanged = false;

                saveList.Add(item.DeviceId);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Add to DeviceList
                    var deviceItem = new DeviceItem(item.Connection);
                    deviceItem.Start();
                    DeviceItems.Add(deviceItem);
                }));
            }

            SaveDeviceList(saveList);
        }

        private void DeviceList_UncheckAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var item in DeviceListItems)
            {
                item.SuppressCheckedChanged = true;
                item.Checked = false;
                item.SuppressCheckedChanged = false;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Remove from DeviceList
                    int i = DeviceItems.ToList().FindIndex(o => o.DeviceId == item.DeviceId);
                    if (i >= 0)
                    {
                        DeviceItems[i].Stop();
                        DeviceItems.RemoveAt(i);
                    }
                }));
            }

            SaveDeviceList(new List<string>());
        }


        private void LoadWindow()
        {
            var w = Properties.Settings.Default.WindowWidth;
            var h = Properties.Settings.Default.WindowHeight;

            var x = Properties.Settings.Default.WindowTop;
            var y = Properties.Settings.Default.WindowLeft;

            if (w > MinWidth && h > MinHeight &&
                x >= 0 && x < SystemParameters.PrimaryScreenWidth &&
                y >= 0 && y < SystemParameters.PrimaryScreenHeight)
            {
                Width = w;
                Height = h;

                Top = x;
                Left = y;
            }
            else
            {
                DockRight();
            }
        }

        private void SaveWindow()
        {
            Properties.Settings.Default.WindowWidth = Width;
            Properties.Settings.Default.WindowHeight = Height;

            Properties.Settings.Default.WindowTop = Top;
            Properties.Settings.Default.WindowLeft = Left;

            Properties.Settings.Default.Save();
        }

        private void LoadUser()
        {
            Username = Properties.Settings.Default.Username;
            _apiToken = Properties.Settings.Default.ApiToken;
            ApiInterval = Math.Min(500, Properties.Settings.Default.ApiInterval);

            if (!string.IsNullOrEmpty(_apiToken)) Login(_apiToken);
        }

        private void SaveUser()
        {
            Properties.Settings.Default.Username = Username;
            Properties.Settings.Default.ApiToken = _apiToken;
            Properties.Settings.Default.ApiInterval = ApiInterval;
            Properties.Settings.Default.Save();
        }

        private void DeleteUser()
        {
            Properties.Settings.Default.Username = null;
            Properties.Settings.Default.ApiToken = null;
            Properties.Settings.Default.Save();
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

        private void ShadedPanel_MouseDown(object sender, MouseButtonEventArgs e) { HideMenu(); }


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

    }
}
