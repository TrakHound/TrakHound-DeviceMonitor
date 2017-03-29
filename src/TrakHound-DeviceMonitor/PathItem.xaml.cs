// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TrakHound.Api.v2.Data;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for PathItem.xaml
    /// </summary>
    public partial class PathItem : UserControl
    {
        public string ToolId { get; set; }

        public string BlockId { get; set; }

        public string LineId { get; set; }


        public string Tool
        {
            get { return (string)GetValue(ToolProperty); }
            set { SetValue(ToolProperty, value); }
        }

        public static readonly DependencyProperty ToolProperty =
            DependencyProperty.Register("Tool", typeof(string), typeof(PathItem), new PropertyMetadata(null));


        public string Block
        {
            get { return (string)GetValue(BlockProperty); }
            set { SetValue(BlockProperty, value); }
        }

        public static readonly DependencyProperty BlockProperty =
            DependencyProperty.Register("Block", typeof(string), typeof(PathItem), new PropertyMetadata(null));

        public string Line
        {
            get { return (string)GetValue(LineProperty); }
            set { SetValue(LineProperty, value); }
        }

        public static readonly DependencyProperty LineProperty =
            DependencyProperty.Register("Line", typeof(string), typeof(PathItem), new PropertyMetadata(null));


        public PathItem(ComponentModel path)
        {
            Init();

            // Tool
            var obj = path.DataItems.Find(o => o.Type == "TOOL_ID" || o.Type == "TOOL_NUMBER");
            if (obj != null) ToolId = obj.Id;

            // Block
            obj = path.DataItems.Find(o => o.Type == "BLOCK");
            if (obj != null) BlockId = obj.Id;

            // Line
            obj = path.DataItems.Find(o => o.Type == "LINE");
            if (obj != null) LineId = obj.Id;
        }


        public PathItem()
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
            // Tool
            if (sample.Id == ToolId)
            {
                if (sample.CDATA != "UNAVAILABLE") Tool = sample.CDATA;
                else Tool = null;
            }

            // Block
            if (sample.Id == BlockId)
            {
                if (sample.CDATA != "UNAVAILABLE") Block = sample.CDATA;
                else Block = null;
            }

            // Line
            if (sample.Id == LineId)
            {
                if (sample.CDATA != "UNAVAILABLE") Line = sample.CDATA;
                else Line = null;
            }
        }

        public List<string> GetIds()
        {
            var l = new List<string>();
            if (ToolId != null) l.Add(ToolId);
            if (BlockId != null) l.Add(BlockId);
            if (LineId != null) l.Add(LineId);
            return l;
        }
    }
}
