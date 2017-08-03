// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TrakHound.TempServer.Menu
{
    public class SystemTrayMenu : ApplicationContext
    {
        private const string SERVICE_NAME = "TrakHound-TempServer";

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static ToolStripMenuItem StatusMenuItem = new ToolStripMenuItem() { Enabled = false };

        public static NotifyIcon NotifyIcon = new NotifyIcon();


        public SystemTrayMenu()
        {
            // Create ContextMenu
            var menu = new ContextMenuStrip();
            menu.Items.Add(StatusMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Start", Properties.Resources.UAC_01, Start));
            menu.Items.Add(new ToolStripMenuItem("Stop", Properties.Resources.UAC_01, Stop));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Open Directory", null, OpenDirectory));
            menu.Items.Add(new ToolStripMenuItem("Open Log File", null, OpenLogFile));
            menu.Items.Add(new ToolStripMenuItem("Run as Console", Properties.Resources.UAC_01, RunAsConsole));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));

            // Set NotifyIcon Properties
            NotifyIcon.Text = "TrakHound TempServer";
            NotifyIcon.Icon = Properties.Resources.tempserver_status_stopped_02;
            NotifyIcon.ContextMenuStrip = menu;
            NotifyIcon.Visible = true;
        }

        private void Start(object sender, EventArgs e)
        {
            try
            {
                var info = new ProcessStartInfo("sc");
                info.Arguments = "start " + SERVICE_NAME;
                info.WindowStyle = ProcessWindowStyle.Hidden;

                //Vista or higher check (Run as Administrator)
                if (Environment.OSVersion.Version.Major >= 6) info.Verb = "runas";

                Process.Start(info);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void Stop(object sender, EventArgs e)
        {
            try
            {
                var info = new ProcessStartInfo("sc");
                info.Arguments = "stop " + SERVICE_NAME;
                info.WindowStyle = ProcessWindowStyle.Hidden;

                //Vista or higher check (Run as Administrator)
                if (Environment.OSVersion.Version.Major >= 6) info.Verb = "runas";

                Process.Start(info);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void OpenDirectory(object sender, EventArgs e)
        {
            try
            {
                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (Directory.Exists(appDir)) Process.Start(appDir);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void OpenLogFile(object sender, EventArgs e)
        {
            try
            {
                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string configPath = Path.Combine(appDir, "error.log");

                if (File.Exists(configPath)) Process.Start(configPath);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void RunAsConsole(object sender, EventArgs e)
        {
            if (Program.ServiceStatus == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                try
                {
                    string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string exePath = Path.Combine(appDir, "TrakHound-TempServer.exe");

                    if (File.Exists(exePath))
                    {
                            var info = new ProcessStartInfo(exePath);
                            info.Arguments = "debug";

                            //Vista or higher check (Run as Administrator)
                            if (Environment.OSVersion.Version.Major >= 6) info.Verb = "runas";

                            Process.Start(info);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            else
            {
                MessageBox.Show("Service is Running. Stop service before running as console.", "Service Not Stopped");
            }
        }

        public static void SetHeader(string text)
        {
            StatusMenuItem.Text = text;
        }

        private void Exit(object sender, EventArgs e)
        {
            Exit();

            Program.Exit();
        }

        public void Exit()
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            NotifyIcon.Visible = false;
            NotifyIcon.Dispose();
        }
    }
}
