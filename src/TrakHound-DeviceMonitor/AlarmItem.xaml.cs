using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TrakHound.Api.v2.Data;

namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for AlarmItem.xaml
    /// </summary>
    public partial class AlarmItem : UserControl
    {
        public string Id { get; set; }


        public string DataItemId
        {
            get { return (string)GetValue(DataItemIdProperty); }
            set { SetValue(DataItemIdProperty, value); }
        }

        public static readonly DependencyProperty DataItemIdProperty =
            DependencyProperty.Register("DataItemId", typeof(string), typeof(AlarmItem), new PropertyMetadata(null));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(AlarmItem), new PropertyMetadata(null));

        public string Condition
        {
            get { return (string)GetValue(ConditionProperty); }
            set { SetValue(ConditionProperty, value); }
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register("Condition", typeof(string), typeof(AlarmItem), new PropertyMetadata(null));

        public string Timestamp
        {
            get { return (string)GetValue(TimestampProperty); }
            set { SetValue(TimestampProperty, value); }
        }

        public static readonly DependencyProperty TimestampProperty =
            DependencyProperty.Register("Timestamp", typeof(string), typeof(AlarmItem), new PropertyMetadata(null));


        public AlarmItem(Alarm alarm)
        {
            Init();

            Id = alarm.Id;
            DataItemId = alarm.DataItemId;
            Condition = alarm.Condition;
            Message = alarm.Message;
            Timestamp = alarm.Timestamp.ToLongTimeString();
        }

        public AlarmItem()
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
