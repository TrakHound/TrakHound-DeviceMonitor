// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for ProgramItem.xaml
    /// </summary>
    public partial class ProgramItem : UserControl
    {
        public string ProgramId { get; set; }

        public string FeedrateOverrideId { get; set; }

        private ObservableCollection<PathItem> _pathItems;
        public ObservableCollection<PathItem> PathItems
        {
            get
            {
                if (_pathItems == null) _pathItems = new ObservableCollection<PathItem>();
                return _pathItems;
            }
            set
            {
                _pathItems = value;
            }
        }

        public string Program
        {
            get { return (string)GetValue(ProgramProperty); }
            set { SetValue(ProgramProperty, value); }
        }

        public static readonly DependencyProperty ProgramProperty =
            DependencyProperty.Register("Program", typeof(string), typeof(ProgramItem), new PropertyMetadata(null));

        public double FeedrateOverride
        {
            get { return (double)GetValue(FeedrateOverrideProperty); }
            set { SetValue(FeedrateOverrideProperty, value); }
        }

        public static readonly DependencyProperty FeedrateOverrideProperty =
            DependencyProperty.Register("FeedrateOverride", typeof(double), typeof(ProgramItem), new PropertyMetadata(-1d));


        public ProgramItem(ComponentModel path)
        {
            Init();

            // Program
            var obj = path.DataItems.Find(o => o.Type == "PROGRAM");
            if (obj != null) ProgramId = obj.Id;

            // Path Feedrate Override
            obj = path.DataItems.Find(o => o.Type == "PATH_FEEDRATE_OVERRIDE" || (o.Type == "PATH_FEEDRATE" && o.Units == "PERCENT"));
            if (obj != null) FeedrateOverrideId = obj.Id;

            PathItems.Add(new PathItem(path));
        }


        public ProgramItem()
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Update(Sample sample)
        {
            // Program
            if (sample.Id == ProgramId)
            {
                if (sample.CDATA != "UNAVAILABLE") Program = sample.CDATA;
                else Program = null;
            }

            // Feedrate Override
            if (sample.Id == FeedrateOverrideId)
            {
                if (sample.CDATA != "UNAVAILABLE")
                {
                    double fovr = 0;
                    if (double.TryParse(sample.CDATA, out fovr))
                    {
                        FeedrateOverride = fovr / 100;
                    }
                }
                else FeedrateOverride = -1;
            }

            // Update PathItems
            foreach (var pathItem in PathItems) pathItem.Update(sample);
        }

        public List<string> GetIds()
        {
            var l = new List<string>();
            if (ProgramId != null) l.Add(ProgramId);
            if (FeedrateOverrideId != null) l.Add(FeedrateOverrideId);
            foreach (var pathItem in PathItems) l.AddRange(pathItem.GetIds());
            return l;
        }
    }
}
