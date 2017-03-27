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


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var connections = Connections.Get("http://localhost");
            foreach (var connection in connections)
            {
                var deviceItem = new DeviceItem(connection.DeviceId);
                deviceItem.Start();
                DeviceItems.Add(deviceItem);
            }

        }


    }
}
