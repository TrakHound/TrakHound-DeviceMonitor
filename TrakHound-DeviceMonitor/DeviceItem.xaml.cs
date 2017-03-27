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
using Requests = TrakHound.Api.v2.Requests;
using System.Threading;

namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        private ManualResetEvent stop;
        private int interval = 5000;
        private string _deviceId;
        private string executionId;
        private string controllerModeId;
        private string programId;
        private string blockId;
        private string lineId;



        public bool Connected
        {
            get { return (bool)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(DeviceItem), new PropertyMetadata(false));


        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        public static readonly DependencyProperty DeviceIdProperty =
            DependencyProperty.Register("DeviceId", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set { SetValue(DeviceNameProperty, value); }
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register("DeviceName", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Manufacturer
        {
            get { return (string)GetValue(ManufacturerProperty); }
            set { SetValue(ManufacturerProperty, value); }
        }

        public static readonly DependencyProperty ManufacturerProperty =
            DependencyProperty.Register("Manufacturer", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Model
        {
            get { return (string)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Serial
        {
            get { return (string)GetValue(SerialProperty); }
            set { SetValue(SerialProperty, value); }
        }

        public static readonly DependencyProperty SerialProperty =
            DependencyProperty.Register("Serial", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string EmergencyStop
        {
            get { return (string)GetValue(EmergencyStopProperty); }
            set { SetValue(EmergencyStopProperty, value); }
        }

        public static readonly DependencyProperty EmergencyStopProperty =
            DependencyProperty.Register("EmergencyStop", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Execution
        {
            get { return (string)GetValue(ExecutionProperty); }
            set { SetValue(ExecutionProperty, value); }
        }

        public static readonly DependencyProperty ExecutionProperty =
            DependencyProperty.Register("Execution", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string ControllerMode
        {
            get { return (string)GetValue(ControllerModeProperty); }
            set { SetValue(ControllerModeProperty, value); }
        }

        public static readonly DependencyProperty ControllerModeProperty =
            DependencyProperty.Register("ControllerMode", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Program
        {
            get { return (string)GetValue(ProgramProperty); }
            set { SetValue(ProgramProperty, value); }
        }

        public static readonly DependencyProperty ProgramProperty =
            DependencyProperty.Register("Program", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Block
        {
            get { return (string)GetValue(BlockProperty); }
            set { SetValue(BlockProperty, value); }
        }

        public static readonly DependencyProperty BlockProperty =
            DependencyProperty.Register("Block", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Line
        {
            get { return (string)GetValue(LineProperty); }
            set { SetValue(LineProperty, value); }
        }

        public static readonly DependencyProperty LineProperty =
            DependencyProperty.Register("Line", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string DeviceStatus
        {
            get { return (string)GetValue(DeviceStatusProperty); }
            set { SetValue(DeviceStatusProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusProperty =
            DependencyProperty.Register("DeviceStatus", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));




        public DeviceItem()
        {
            Init();
        }

        public DeviceItem(string deviceId)
        {
            Init();
            DeviceId = deviceId;
            _deviceId = deviceId;

        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem(new WaitCallback(Worker));
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
        }

        private void Worker(object o)
        {
            GetModel();
            bool previousConnected = false;

            while (!stop.WaitOne(interval, true))
            {
                var status = Requests.Status.Get("http://localhost", _deviceId);
                if (status != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => { Connected = status.Connected; }));

                    if (status.Connected && !previousConnected)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(StartSamplesStream));
                        ThreadPool.QueueUserWorkItem(new WaitCallback(StartActivityStream));
                    }

                    previousConnected = status.Connected;
                }
            }
        }

        private void GetModel()
        {
            var model = Requests.Model.Get("http://localhost", _deviceId);
            if (model != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeviceName = model.Name;
                    Manufacturer = model.Manufacturer;
                    Model = model.Model;
                    Serial = model.SerialNumber;

                    var dataItems = model.GetDataItems();

                    var obj = dataItems.Find(o => o.Type == "EXECUTION");
                    if (obj != null) executionId = obj.Id;

                    obj = dataItems.Find(o => o.Type == "CONTROLLER_MODE");
                    if (obj != null) controllerModeId = obj.Id;

                    obj = dataItems.Find(o => o.Type == "PROGRAM");
                    if (obj != null) programId = obj.Id;

                    obj = dataItems.Find(o => o.Type == "BLOCK");
                    if (obj != null) blockId = obj.Id;

                    obj = dataItems.Find(o => o.Type == "LINE");
                    if (obj != null) lineId = obj.Id;
                }));
            }
        }

        private void UpdateActivity()
        {
            var activity = Requests.Activity.Get("http://localhost", _deviceId);
            if (activity != null)
            {
                if (activity.Events != null)
                {
                    // Get EmergencyStop
                    var estop = activity.Events.Find(o => o.Name == "Emergency Stop");
                    if (estop != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() => { EmergencyStop = estop.Value; }));
                    }

                    // Get Status
                    var status = activity.Events.Find(o => o.Name == "Status");
                    if (status != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() => { DeviceStatus = status.Value; }));
                    }
                }
            }
        }

        private void StartActivityStream(object o)
        {
            var stream = new Requests.Activity.Stream("http://localhost", _deviceId, 1000);
            stream.ActivityReceived += Stream_ActivityReceived;
            stream.Start();
        }

        private void Stream_ActivityReceived(Api.v2.Data.ActivityItem activity)
        {
            if (activity.Events != null)
            {
                // Get EmergencyStop
                var estop = activity.Events.Find(o => o.Name == "Emergency Stop");
                if (estop != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => { EmergencyStop = estop.Value; }));
                }

                // Get Status
                var status = activity.Events.Find(o => o.Name == "Status");
                if (status != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => { DeviceStatus = status.Value; }));
                }
            }
        }

        private void StartSamplesStream(object o)
        {
            var stream = new Requests.Samples.Stream("http://localhost", _deviceId, 1000);
            stream.SampleReceived += Stream_SampleReceived;
            stream.Start();
        }

        private void Stream_SampleReceived(Api.v2.Data.Sample sample)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                if (sample.Id == executionId) Execution = sample.CDATA;

                if (sample.Id == controllerModeId) ControllerMode = sample.CDATA;

                if (sample.Id == programId) Program = sample.CDATA;

                if (sample.Id == blockId) Block = sample.CDATA;

                if (sample.Id == lineId) Line = sample.CDATA;

            }));
        }


        private void UpdateSamples()
        {
            var samples = Requests.Samples.Get("http://localhost", _deviceId);
            if (samples != null)
            {
                Dispatcher.BeginInvoke(new Action(() => {

                    // Get Execution
                    var obj = samples.Find(o => o.Id == executionId);
                    if (obj != null) Execution = obj.CDATA;

                    // Get Controller Mode
                    obj = samples.Find(o => o.Id == controllerModeId);
                    if (obj != null) ControllerMode = obj.CDATA;

                    // Get Program
                    obj = samples.Find(o => o.Id == programId);
                    if (obj != null) Program = obj.CDATA;

                    // Get Block
                    obj = samples.Find(o => o.Id == blockId);
                    if (obj != null) Block = obj.CDATA;

                    // Get Line
                    obj = samples.Find(o => o.Id == lineId);
                    if (obj != null) Line = obj.CDATA;

                }));
            }
        }
    }
}
