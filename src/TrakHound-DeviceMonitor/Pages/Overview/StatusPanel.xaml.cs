// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;
using System.Linq;

namespace TrakHound.DeviceMonitor.Pages.Overview
{
    /// <summary>
    /// Interaction logic for OverviewStatusPanel.xaml
    /// </summary>
    public partial class StatusPanel : UserControl
    {
        private System.Timers.Timer statusTimer;


        public string PathId { get; set; }

        public string DeviceId { get; set; }

        public string AvailabilityId { get; set; }

        public string EmergencyStopId { get; set; }

        public string ExecutionId { get; set; }

        public string ControllerModeId { get; set; }

        public string MessageId { get; set; }

        public string ProgramNameId { get; set; }

        public string FeedrateOverrideId { get; set; }


        public List<string> AlarmIds = new List<string>();


        public DateTime DeviceStatusTime { get; set; }

        public DateTime ProgramStatusTime { get; set; }


        #region "Dependency Properties"

        private ObservableCollection<PathPanel> _pathPanels;
        public ObservableCollection<PathPanel> PathPanels
        {
            get
            {
                if (_pathPanels == null) _pathPanels = new ObservableCollection<PathPanel>();
                return _pathPanels;
            }
            set
            {
                _pathPanels = value;
            }
        }

        private ObservableCollection<AlarmPanel> _alarmPanels;
        public ObservableCollection<AlarmPanel> AlarmPanels
        {
            get
            {
                if (_alarmPanels == null) _alarmPanels = new ObservableCollection<AlarmPanel>();
                return _alarmPanels;
            }
            set
            {
                _alarmPanels = value;
            }
        }

        #region "OEE"

        public bool UsePerformance
        {
            get { return (bool)GetValue(UsePerformanceProperty); }
            set { SetValue(UsePerformanceProperty, value); }
        }

        public static readonly DependencyProperty UsePerformanceProperty =
            DependencyProperty.Register("UsePerformance", typeof(bool), typeof(StatusPanel), new PropertyMetadata(false));

        public bool UseQuality
        {
            get { return (bool)GetValue(UseQualityProperty); }
            set { SetValue(UseQualityProperty, value); }
        }

        public static readonly DependencyProperty UseQualityProperty =
            DependencyProperty.Register("UseQuality", typeof(bool), typeof(StatusPanel), new PropertyMetadata(false));


        public double OeeValue
        {
            get { return (double)GetValue(OeeValueProperty); }
            set { SetValue(OeeValueProperty, value); }
        }

        public static readonly DependencyProperty OeeValueProperty =
            DependencyProperty.Register("OeeValue", typeof(double), typeof(StatusPanel), new PropertyMetadata(0D));


        public int OeeStatus
        {
            get { return (int)GetValue(OeeStatusProperty); }
            set { SetValue(OeeStatusProperty, value); }
        }

        public static readonly DependencyProperty OeeStatusProperty =
            DependencyProperty.Register("OeeStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public double AvailabilityValue
        {
            get { return (double)GetValue(AvailabilityValueProperty); }
            set { SetValue(AvailabilityValueProperty, value); }
        }

        public static readonly DependencyProperty AvailabilityValueProperty =
            DependencyProperty.Register("AvailabilityValue", typeof(double), typeof(StatusPanel), new PropertyMetadata(0D));

        public int AvailabilityStatus
        {
            get { return (int)GetValue(AvailabilityStatusProperty); }
            set { SetValue(AvailabilityStatusProperty, value); }
        }

        public static readonly DependencyProperty AvailabilityStatusProperty =
            DependencyProperty.Register("AvailabilityStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public double PerformanceValue
        {
            get { return (double)GetValue(PerformanceValueProperty); }
            set { SetValue(PerformanceValueProperty, value); }
        }

        public static readonly DependencyProperty PerformanceValueProperty =
            DependencyProperty.Register("PerformanceValue", typeof(double), typeof(StatusPanel), new PropertyMetadata(0D));

        public int PerformanceStatus
        {
            get { return (int)GetValue(PerformanceStatusProperty); }
            set { SetValue(PerformanceStatusProperty, value); }
        }

        public static readonly DependencyProperty PerformanceStatusProperty =
            DependencyProperty.Register("PerformanceStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public double QualityValue
        {
            get { return (double)GetValue(QualityValueProperty); }
            set { SetValue(QualityValueProperty, value); }
        }

        public static readonly DependencyProperty QualityValueProperty =
            DependencyProperty.Register("QualityValue", typeof(double), typeof(StatusPanel), new PropertyMetadata(0D));

        public int QualityStatus
        {
            get { return (int)GetValue(QualityStatusProperty); }
            set { SetValue(QualityStatusProperty, value); }
        }

        public static readonly DependencyProperty QualityStatusProperty =
            DependencyProperty.Register("QualityStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public int TotalPartCount   
        {
            get { return (int)GetValue(TotalPartCountProperty); }
            set { SetValue(TotalPartCountProperty, value); }
        }

        public static readonly DependencyProperty TotalPartCountProperty =
            DependencyProperty.Register("TotalPartCount", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));

        public int GoodPartCount
        {
            get { return (int)GetValue(GoodPartCountProperty); }
            set { SetValue(GoodPartCountProperty, value); }
        }

        public static readonly DependencyProperty GoodPartCountProperty =
            DependencyProperty.Register("GoodPartCount", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));

        #endregion

        #region "Status"

        public string Availability
        {
            get { return (string)GetValue(AvailabilityProperty); }
            set { SetValue(AvailabilityProperty, value); }
        }

        public static readonly DependencyProperty AvailabilityProperty =
            DependencyProperty.Register("Availability", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));


        public string FunctionalMode
        {
            get { return (string)GetValue(FunctionalModeProperty); }
            set { SetValue(FunctionalModeProperty, value); }
        }

        public static readonly DependencyProperty FunctionalModeProperty =
            DependencyProperty.Register("FunctionalMode", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));




        public string DeviceStatus
        {
            get { return (string)GetValue(DeviceStatusProperty); }
            set { SetValue(DeviceStatusProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusProperty =
            DependencyProperty.Register("DeviceStatus", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        public string DeviceStatusDescription
        {
            get { return (string)GetValue(DeviceStatusDescriptionProperty); }
            set { SetValue(DeviceStatusDescriptionProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusDescriptionProperty =
            DependencyProperty.Register("DeviceStatusDescription", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        public TimeSpan DeviceStatusTimer
        {
            get { return (TimeSpan)GetValue(DeviceStatusTimerProperty); }
            set { SetValue(DeviceStatusTimerProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusTimerProperty =
            DependencyProperty.Register("DeviceStatusTimer", typeof(TimeSpan), typeof(StatusPanel), new PropertyMetadata(TimeSpan.Zero));


        public string ProgramStatus
        {
            get { return (string)GetValue(ProgramStatusProperty); }
            set { SetValue(ProgramStatusProperty, value); }
        }

        public static readonly DependencyProperty ProgramStatusProperty =
            DependencyProperty.Register("ProgramStatus", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        public string ProgramStatusDescription
        {
            get { return (string)GetValue(ProgramStatusDescriptionProperty); }
            set { SetValue(ProgramStatusDescriptionProperty, value); }
        }

        public static readonly DependencyProperty ProgramStatusDescriptionProperty =
            DependencyProperty.Register("ProgramStatusDescription", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        public TimeSpan ProgramStatusTimer
        {
            get { return (TimeSpan)GetValue(ProgramStatusTimerProperty); }
            set { SetValue(ProgramStatusTimerProperty, value); }
        }

        public static readonly DependencyProperty ProgramStatusTimerProperty =
            DependencyProperty.Register("ProgramStatusTimer", typeof(TimeSpan), typeof(StatusPanel), new PropertyMetadata(TimeSpan.Zero));

        public string ProgramName
        {
            get { return (string)GetValue(ProgramNameProperty); }
            set { SetValue(ProgramNameProperty, value); }
        }

        public static readonly DependencyProperty ProgramNameProperty =
            DependencyProperty.Register("ProgramName", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));



        public string EmergencyStop
        {
            get { return (string)GetValue(EmergencyStopProperty); }
            set { SetValue(EmergencyStopProperty, value); }
        }

        public static readonly DependencyProperty EmergencyStopProperty =
            DependencyProperty.Register("EmergencyStop", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        public string Execution
        {
            get { return (string)GetValue(ExecutionProperty); }
            set { SetValue(ExecutionProperty, value); }
        }

        public static readonly DependencyProperty ExecutionProperty =
            DependencyProperty.Register("Execution", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));


        public string ControllerMode
        {
            get { return (string)GetValue(ControllerModeProperty); }
            set { SetValue(ControllerModeProperty, value); }
        }

        public static readonly DependencyProperty ControllerModeProperty =
            DependencyProperty.Register("ControllerMode", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(StatusPanel), new PropertyMetadata(null));

        #endregion

        #region "Overrides"

        public double FeedrateOverride
        {
            get { return (double)GetValue(FeedrateOverrideProperty); }
            set { SetValue(FeedrateOverrideProperty, value); }
        }

        public static readonly DependencyProperty FeedrateOverrideProperty =
            DependencyProperty.Register("FeedrateOverride", typeof(double), typeof(StatusPanel), new PropertyMetadata(null));

        public int FeedrateOverrideStatus
        {
            get { return (int)GetValue(FeedrateOverrideStatusProperty); }
            set { SetValue(FeedrateOverrideStatusProperty, value); }
        }

        public static readonly DependencyProperty FeedrateOverrideStatusProperty =
            DependencyProperty.Register("FeedrateOverrideStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public double RapidOverride
        {
            get { return (double)GetValue(RapidOverrideProperty); }
            set { SetValue(RapidOverrideProperty, value); }
        }

        public static readonly DependencyProperty RapidOverrideProperty =
            DependencyProperty.Register("RapidOverride", typeof(double), typeof(StatusPanel), new PropertyMetadata(null));

        public int RapidOverrideStatus
        {
            get { return (int)GetValue(RapidOverrideStatusProperty); }
            set { SetValue(RapidOverrideStatusProperty, value); }
        }

        public static readonly DependencyProperty RapidOverrideStatusProperty =
            DependencyProperty.Register("RapidOverrideStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));


        public double JogOverride
        {
            get { return (double)GetValue(JogOverrideProperty); }
            set { SetValue(JogOverrideProperty, value); }
        }

        public static readonly DependencyProperty JogOverrideProperty =
            DependencyProperty.Register("JogOverride", typeof(double), typeof(StatusPanel), new PropertyMetadata(null));

        public int JogOverrideStatus
        {
            get { return (int)GetValue(JogOverrideStatusProperty); }
            set { SetValue(JogOverrideStatusProperty, value); }
        }

        public static readonly DependencyProperty JogOverrideStatusProperty =
            DependencyProperty.Register("JogOverrideStatus", typeof(int), typeof(StatusPanel), new PropertyMetadata(0));

        #endregion

        #endregion

        public StatusPanel()
        {
            Init();
        }

        public StatusPanel(string deviceId, ComponentModel path)
        {
            Init();

            PathId = path.Id;
            DeviceId = deviceId;

            // Get Execution ID
            var obj = path.DataItems.Find(o => o.Type == "EXECUTION");
            if (obj != null) ExecutionId = obj.Id;

            // Get Controller Mode ID
            obj = path.DataItems.Find(o => o.Type == "CONTROLLER_MODE");
            if (obj != null) ControllerModeId = obj.Id;

            // Get Program Name ID
            obj = path.DataItems.Find(o => o.Type == "PROGRAM");
            if (obj != null) ProgramNameId = obj.Id;

            // Get Message ID
            obj = path.DataItems.Find(o => o.Type == "MESSAGE");
            if (obj != null) MessageId = obj.Id;

            // Feedrate Override
            obj = path.DataItems.Find(o => o.Type == "PATH_FEEDRATE_OVERRIDE" || (o.Type == "PATH_FEEDRATE" && o.Units == "PERCENT"));
            if (obj != null) FeedrateOverrideId = obj.Id;

            // Get Alarm Condition IDs
            AlarmIds.AddRange(path.DataItems.FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

            PathPanels.Add(new PathPanel(path));
        }

        public void Init()
        {
            InitializeComponent();
            DataContext = this;

            statusTimer = new System.Timers.Timer();
            statusTimer.Interval = 500;
            statusTimer.Elapsed += DeviceStatusTimer_Elapsed;
            statusTimer.Enabled = true;
        }

        private void DeviceStatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DeviceStatusTime > DateTime.MinValue)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeviceStatusTimer = DateTime.UtcNow - DeviceStatusTime;
                    ProgramStatusTimer = DateTime.UtcNow - ProgramStatusTime;
                }));
            }
        }

        public void Update(Sample sample)
        {
            // Availability
            if (sample.Id == AvailabilityId) Availability = sample.CDATA;

            // Emergency Stop
            if (sample.Id == EmergencyStopId) EmergencyStop = sample.CDATA;

            // Execution
            if (sample.Id == ExecutionId) Execution = sample.CDATA;

            // ControllerMode
            if (sample.Id == ControllerModeId) ControllerMode = sample.CDATA;

            // Message
            if (sample.Id == MessageId) Message = sample.CDATA;

            // Program
            if (sample.Id == ProgramNameId) ProgramName = sample.CDATA;

            // Clear Alarms if found
            foreach (var alarmId in AlarmIds)
            {
                int i = AlarmPanels.ToList().FindIndex(o => o.DataItemId == sample.Id);
                if (i >= 0 && (sample.Condition == "NORMAL" || sample.Condition == "UNAVAILABLE"))
                {
                    AlarmPanels.RemoveAt(i);
                }
            }

            // Feedrate Override
            if (sample.Id == FeedrateOverrideId)
            {
                if (sample.CDATA != "UNAVAILABLE")
                {
                    double ovr = 0;
                    if (double.TryParse(sample.CDATA, out ovr))
                    {
                        FeedrateOverride = ovr / 100;
                    }
                }
                else FeedrateOverride = -1;

                if (FeedrateOverride > 0.90) FeedrateOverrideStatus = 3;
                else if (FeedrateOverride > 0.50) FeedrateOverrideStatus = 2;
                else if (FeedrateOverride > 0) FeedrateOverrideStatus = 1;
                else FeedrateOverrideStatus = 0;
            }

            foreach (var pathPanel in PathPanels) pathPanel.Update(sample);
        }

        public void Update(Oee oee)
        {
            var config = Properties.Settings.Default.DeviceList.Find(o => o.DeviceId == DeviceId);
            if (config != null)
            {
                UsePerformance = config.PerformanceEnabled;
                UseQuality = config.QualityEnabled;
            }

            double availability = 0;
            if (oee.Availability != null) availability = oee.Availability.Value;

            double performance = 0;
            if (oee.Performance != null) performance = oee.Performance.Value;

            double quality = 0;
            if (oee.Quality != null) quality = oee.Quality.Value;

            int partCount = 0;
            int goodPartCount = 0;
            if (oee.Quality != null)
            {
                partCount = oee.Quality.TotalParts;
                goodPartCount = oee.Quality.GoodParts;
            }

            // Set OEE         
            if (UsePerformance && UseQuality) OeeValue = availability * performance * quality;
            else if (UsePerformance) OeeValue = availability * performance;
            else if (UseQuality) OeeValue = availability * quality;
            else OeeValue = availability;
            if (OeeValue > 0.70) OeeStatus = 3;
            else if (OeeValue > 0.50) OeeStatus = 2;
            else if (OeeValue > 0) OeeStatus = 1;
            else OeeStatus = -1;

            // Set Availability
            AvailabilityValue = availability;
            if (AvailabilityValue > 0.70) AvailabilityStatus = 3;
            else if (AvailabilityValue > 0.50) AvailabilityStatus = 2;
            else if (AvailabilityValue > 0) AvailabilityStatus = 1;
            else AvailabilityStatus = -1;

            // Set Performance
            PerformanceValue = performance;
            if (PerformanceValue > 0.70) PerformanceStatus = 3;
            else if (PerformanceValue > 0.50) PerformanceStatus = 2;
            else if (PerformanceValue > 0) PerformanceStatus = 1;
            else PerformanceStatus = -1;

            // Set Quality
            QualityValue = quality;
            if (QualityValue > 0.70) QualityStatus = 3;
            else if (QualityValue > 0.50) QualityStatus = 2;
            else if (QualityValue > 0) QualityStatus = 1;
            else QualityStatus = -1;

            // Set Part Count
            TotalPartCount = partCount;
            GoodPartCount = goodPartCount;
        }

        public void Update(Alarm alarm)
        {
            int i = AlarmPanels.ToList().FindIndex(o => o.DataItemId == alarm.DataItemId);
            if (i >= 0) AlarmPanels[i] = new AlarmPanel(alarm);
            else AlarmPanels.Add(new AlarmPanel(alarm));
        }

        public List<string> GetIds()
        {
            var l = new List<string>();
            if (AvailabilityId != null) l.Add(AvailabilityId);
            if (EmergencyStopId != null) l.Add(EmergencyStopId);
            if (ExecutionId != null) l.Add(ExecutionId);
            if (ControllerModeId != null) l.Add(ControllerModeId);
            if (ProgramNameId != null) l.Add(ProgramNameId);
            if (MessageId != null) l.Add(MessageId);
            if (FeedrateOverrideId != null) l.Add(FeedrateOverrideId);
            foreach (var pathPanel in PathPanels) l.AddRange(pathPanel.GetIds());
            return l;
        }
    }
}
