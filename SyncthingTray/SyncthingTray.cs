﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SyncthingTray.Properties;

namespace SyncthingTray
{
    public partial class SyncthingTray : Form
    {
        private System.Diagnostics.Process ActiveProcess;
        public SyncthingTray()
        {
            InitializeComponent();
        }

        private void btnSetPath_Click(object sender, EventArgs e)
        {
            var result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            var fileName = openFileDialog.FileName.Trim();
            CheckPath(fileName);
        }
        private void CheckPath(string syncthingPath)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = false;
            chkStartOnBoot.Enabled = false;

            if (string.IsNullOrEmpty(syncthingPath) || !File.Exists(syncthingPath)) return;
            txtPath.Text = syncthingPath;
            Settings.Default.SyncthingPath = syncthingPath;
            Settings.Default.Save();
            var isRunning = IsSyncthingRunning();
            btnStart.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
            chkStartOnBoot.Enabled = true;
        }

        private void timerCheckSync_Tick(object sender, EventArgs e)
        {
            var isRunning = IsSyncthingRunning();
            if (isRunning)
            {
                lblState.Text = "RUNNING";
                lblState.ForeColor = Color.Green;
                notifyIcon.Icon = Icon.ExtractAssociatedIcon(Path.Combine(Application.StartupPath, "Resources", "logo-64.ico"));
            }
            else
            {
                lblState.Text = "NOT RUNNING";
                lblState.ForeColor = Color.Red;
                notifyIcon.Icon = Icon.ExtractAssociatedIcon(Path.Combine(Application.StartupPath, "Resources", "logo-64-grayscale.ico"));
            }
            btnStart.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
            chkStartOnBoot.Enabled = true;
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            CheckPath(txtPath.Text.Trim());
        }

        public static bool IsSyncthingRunning()
        {
            return ProcessHelper.IsProcessOpen("syncthing");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (ActiveProcess != null)
                {
                    ActiveProcess.OutputDataReceived -= ActiveProcess_OutputDataReceived;
                    ActiveProcess.ErrorDataReceived -= ActiveProcess_OutputDataReceived;
                    ActiveProcess.Kill();
                    ActiveProcess = null;
                }
                else
                {
                    ProcessHelper.StopProcess("syncthing");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                ActiveProcess = ProcessHelper.StartProcess(Settings.Default.SyncthingPath);
                ActiveProcess.OutputDataReceived += ActiveProcess_OutputDataReceived;
                ActiveProcess.ErrorDataReceived += ActiveProcess_OutputDataReceived;
                ActiveProcess.BeginOutputReadLine();
                ActiveProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void ActiveProcess_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            this.Invoke(new MethodInvoker(() => textBoxLog.AppendText(e.Data + System.Environment.NewLine)));
        }

        private void SyncthingTray_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyIcon.ShowBalloonTip(1000, "SyncthingTray", "I'm down here if you need me...", ToolTipIcon.Info);
            }
            else
                this.ShowInTaskbar = true;
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = WindowState == FormWindowState.Normal ? FormWindowState.Minimized : FormWindowState.Normal;
        }

        private void showSyncthingTraySettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void chkMinimizeOnStart_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.MinimizeOnStart = chkMinimizeOnStart.Checked;
            Settings.Default.Save();
        }

        private void chkStartOnBoot_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                RegistryHelper.SetStartup(chkStartOnBoot.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SyncthingTray_Shown(object sender, EventArgs e)
        {
            try
            {
                chkStartOnBoot.Checked = RegistryHelper.GetStartup();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            var syncthingPath = Settings.Default.SyncthingPath;
            CheckPath(syncthingPath);

            if (chkStartOnBoot.Checked && btnStart.Enabled && !IsSyncthingRunning())
            {
                btnStart.PerformClick();
            }
        }

        private void SyncthingTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsSyncthingRunning())
            {
                btnStop.PerformClick();
            }
        }

        private void SyncthingTray_Load(object sender, EventArgs e)
        {
            chkMinimizeOnStart.Checked = Settings.Default.MinimizeOnStart;
            if (Settings.Default.MinimizeOnStart)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openWebinterfaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://127.0.0.1:8080");
        }
    }
}
