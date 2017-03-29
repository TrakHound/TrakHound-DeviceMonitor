// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for StatusItem.xaml
    /// </summary>
    public partial class StatusItem : UserControl
    {
        private System.Timers.Timer statusUpdateTimer;


        public string PathId { get; set; }

        public string ExecutionId { get; set; }

        public string ControllerModeId { get; set; }

        public string MessageId { get; set; }


        public DateTime StatusTimestamp { get; set; }

        public List<string> AlarmIds = new List<string>();


        private ObservableCollection<AlarmItem> _alarms;
        public ObservableCollection<AlarmItem> Alarms
        {
            get
            {
                if (_alarms == null) _alarms = new ObservableCollection<AlarmItem>();
                return _alarms;
            }
            set
            {
                _alarms = value;
            }
        }

        public ProgramItem ProgramItem
        {
            get { return (ProgramItem)GetValue(PathItemProperty); }
            set { SetValue(PathItemProperty, value); }
        }

        public static readonly DependencyProperty PathItemProperty =
            DependencyProperty.Register("ProgramItem", typeof(ProgramItem), typeof(StatusItem), new PropertyMetadata(null));


        public bool Connected
        {
            get { return (bool)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(StatusItem), new PropertyMetadata(false));


        public string Execution
        {
            get { return (string)GetValue(ExecutionProperty); }
            set { SetValue(ExecutionProperty, value); }
        }

        public static readonly DependencyProperty ExecutionProperty =
            DependencyProperty.Register("Execution", typeof(string), typeof(StatusItem), new PropertyMetadata(null));


        public string ControllerMode
        {
            get { return (string)GetValue(ControllerModeProperty); }
            set { SetValue(ControllerModeProperty, value); }
        }

        public static readonly DependencyProperty ControllerModeProperty =
            DependencyProperty.Register("ControllerMode", typeof(string), typeof(StatusItem), new PropertyMetadata(null));


        public string DeviceStatus
        {
            get { return (string)GetValue(DeviceStatusProperty); }
            set { SetValue(DeviceStatusProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusProperty =
            DependencyProperty.Register("DeviceStatus", typeof(string), typeof(StatusItem), new PropertyMetadata(null));


        public TimeSpan DeviceStatusTime
        {
            get { return (TimeSpan)GetValue(DeviceStatusTimeProperty); }
            set { SetValue(DeviceStatusTimeProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusTimeProperty =
            DependencyProperty.Register("DeviceStatusTime", typeof(TimeSpan), typeof(StatusItem), new PropertyMetadata(TimeSpan.Zero));


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(StatusItem), new PropertyMetadata(null));



        public StatusItem()
        {
            Init();
        }

        public StatusItem(ComponentModel path)
        {
            Init();

            PathId = path.Id;

            // Get Execution ID
            var obj = path.DataItems.Find(o => o.Type == "EXECUTION");
            if (obj != null) ExecutionId = obj.Id;

            // Get Controller Mode ID
            obj = path.DataItems.Find(o => o.Type == "CONTROLLER_MODE");
            if (obj != null) ControllerModeId = obj.Id;

            // Get Message ID
            obj = path.DataItems.Find(o => o.Type == "MESSAGE");
            if (obj != null) MessageId = obj.Id;

            // Get Alarm Condition IDs
            AlarmIds.AddRange(path.DataItems.FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

            ProgramItem = new ProgramItem(path);
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;

            statusUpdateTimer = new System.Timers.Timer();
            statusUpdateTimer.Interval = 1000;
            statusUpdateTimer.Elapsed += StatusUpdateTimer_Elapsed;
            statusUpdateTimer.Start();
        }

        public void Update(Sample sample)
        {
            // Execution
            if (sample.Id == ExecutionId) Execution = sample.CDATA;

            // ControllerMode
            if (sample.Id == ControllerModeId) ControllerMode = sample.CDATA;

            // Message
            if (sample.Id == MessageId) Message = sample.CDATA;

            // Clear Alarms if found
            foreach (var alarmId in AlarmIds)
            {
                int i = Alarms.ToList().FindIndex(o => o.DataItemId == sample.Id);
                if (i >= 0 && (sample.Condition == "NORMAL" || sample.Condition == "UNAVAILABLE"))
                {
                    Alarms.RemoveAt(i);
                }
            }

            if (ProgramItem != null) ProgramItem.Update(sample);
        }

        public void Update(Alarm alarm)
        {
            int i = Alarms.ToList().FindIndex(o => o.DataItemId == alarm.DataItemId);
            if (i >= 0) Alarms[i] = new AlarmItem(alarm);
            else Alarms.Add(new AlarmItem(alarm));
        }

        private void StatusUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (StatusTimestamp > DateTime.MinValue)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeviceStatusTime = DateTime.UtcNow - StatusTimestamp;
                }));
            }
        }

        public List<string> GetIds()
        {
            var l = new List<string>();
            if (ExecutionId != null) l.Add(ExecutionId);
            if (ControllerModeId != null) l.Add(ControllerModeId);
            if (MessageId != null) l.Add(MessageId);
            if (ProgramItem != null) l.AddRange(ProgramItem.GetIds());
            return l;
        }

    }
}
