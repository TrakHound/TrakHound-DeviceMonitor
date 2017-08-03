// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;

namespace TrakHound.DeviceMonitor.Pages.Overview
{
    /// <summary>
    /// Interaction logic for AlarmPanel.xaml
    /// </summary>
    public partial class AlarmPanel : UserControl
    {
        public string AlarmId { get; set; }

        public string DataItemId
        {
            get { return (string)GetValue(DataItemIdProperty); }
            set { SetValue(DataItemIdProperty, value); }
        }

        public static readonly DependencyProperty DataItemIdProperty =
            DependencyProperty.Register("DataItemId", typeof(string), typeof(AlarmPanel), new PropertyMetadata(null));


        public string Condition
        {
            get { return (string)GetValue(ConditionProperty); }
            set { SetValue(ConditionProperty, value); }
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register("Condition", typeof(string), typeof(AlarmPanel), new PropertyMetadata(null));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(AlarmPanel), new PropertyMetadata(null));


        public AlarmPanel(Alarm alarm)
        {
            InitializeComponent();
            DataContext = this;

            AlarmId = alarm.Id;
            DataItemId = alarm.DataItemId;
            Condition = alarm.Condition;
            Message = alarm.Message;
        }
    }
}
