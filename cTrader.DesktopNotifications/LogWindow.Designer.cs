/*
Nano TimeTracker - a small free windows time-tracking utility
Copyright (C) 2011 Tao Klerks

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

 */

using System;
using System.Drawing;
using System.Windows.Forms;


namespace cTrader.DesktopNotifications
{
    partial class LogWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogWindow));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip_SysTrayContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openLogWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip_DataGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.updateTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGrid = new cTrader.DesktopNotifications.FrameworkClassReplacements.DataGridViewWithRowContextMenu();
            this.StartDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Pair = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Timeframe = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip_SysTrayContext.SuspendLayout();
            this.contextMenuStrip_DataGrid.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // timer_StatusUpdate
            // 
            this.timer_StatusUpdate.Interval = 1000;
            this.timer_StatusUpdate.Tick += new System.EventHandler(this.timer_StatusUpdate_Tick);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip_SysTrayContext;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Time Logger";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            this.notifyIcon1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDown);
            // 
            // contextMenuStrip_SysTrayContext
            // 
            this.contextMenuStrip_SysTrayContext.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip_SysTrayContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openLogWindowToolStripMenuItem,
            this.startTaskToolStripMenuItem,
            this.exitToolStripMenuItem1});
            this.contextMenuStrip_SysTrayContext.Name = "contextMenuStrip_SysTrayContext";
            this.contextMenuStrip_SysTrayContext.Size = new System.Drawing.Size(315, 132);
            // 
            // openLogWindowToolStripMenuItem
            // 
            this.openLogWindowToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openLogWindowToolStripMenuItem.Name = "openLogWindowToolStripMenuItem";
            this.openLogWindowToolStripMenuItem.Size = new System.Drawing.Size(314, 32);
            this.openLogWindowToolStripMenuItem.Text = "Open Log Window...";
            this.openLogWindowToolStripMenuItem.Click += new System.EventHandler(this.openLogWindowToolStripMenuItem_Click);
            // 
            // startTaskToolStripMenuItem
            // 
            this.startTaskToolStripMenuItem.Name = "startTaskToolStripMenuItem";
            this.startTaskToolStripMenuItem.Size = new System.Drawing.Size(314, 32);
            this.startTaskToolStripMenuItem.Text = "Start/Add Logging Entry...";
            this.startTaskToolStripMenuItem.Click += new System.EventHandler(this.startTaskToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(314, 32);
            this.exitToolStripMenuItem1.Text = "Exit";
            this.exitToolStripMenuItem1.Click += new System.EventHandler(this.exitToolStripMenuItem1_Click);
            // 
            // timer_NotifySingleClick
            // 
            this.timer_NotifySingleClick.Interval = 500;
            this.timer_NotifySingleClick.Tick += new System.EventHandler(this.timer_NotifySingleClick_Tick);
            // 
            // contextMenuStrip_DataGrid
            // 
            this.contextMenuStrip_DataGrid.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip_DataGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.updateTaskToolStripMenuItem,
            this.splitTaskToolStripMenuItem,
            this.deleteTaskToolStripMenuItem});
            this.contextMenuStrip_DataGrid.Name = "contextMenuStrip_DataGrid";
            this.contextMenuStrip_DataGrid.Size = new System.Drawing.Size(227, 132);
            // 
            // updateTaskToolStripMenuItem
            // 
            this.updateTaskToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.updateTaskToolStripMenuItem.Name = "updateTaskToolStripMenuItem";
            this.updateTaskToolStripMenuItem.Size = new System.Drawing.Size(226, 32);
            this.updateTaskToolStripMenuItem.Text = "Update Entry...";
            this.updateTaskToolStripMenuItem.Click += new System.EventHandler(this.updateTaskToolStripMenuItem_Click);
            // 
            // splitTaskToolStripMenuItem
            // 
            this.splitTaskToolStripMenuItem.Name = "splitTaskToolStripMenuItem";
            this.splitTaskToolStripMenuItem.Size = new System.Drawing.Size(226, 32);
            this.splitTaskToolStripMenuItem.Text = "Split Entry...";
            // 
            // deleteTaskToolStripMenuItem
            // 
            this.deleteTaskToolStripMenuItem.Name = "deleteTaskToolStripMenuItem";
            this.deleteTaskToolStripMenuItem.Size = new System.Drawing.Size(226, 32);
            this.deleteTaskToolStripMenuItem.Text = "Delete Entry...";
            this.deleteTaskToolStripMenuItem.Click += new System.EventHandler(this.deleteTaskToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(788, 33);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3,
            this.toolStripSeparator1,
            this.closeToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(54, 29);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(267, 6);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(267, 6);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(267, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteLogToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(69, 29);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // deleteLogToolStripMenuItem
            // 
            this.deleteLogToolStripMenuItem.Name = "deleteLogToolStripMenuItem";
            this.deleteLogToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.deleteLogToolStripMenuItem.Text = "Delete Log...";
            this.deleteLogToolStripMenuItem.Click += new System.EventHandler(this.deleteLogToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.optionsToolStripMenuItem.Text = "Options...";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(65, 29);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // dataGrid
            // 
            this.dataGrid.AllowUserToAddRows = false;
            this.dataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StartDateTime,
            this.Pair,
            this.Timeframe});
            this.dataGrid.DataMember = "DataTable1";
            this.dataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            this.dataGrid.Location = new System.Drawing.Point(8, 27);
            this.dataGrid.Name = "dataGrid";
            this.dataGrid.RowContextMenuStrip = this.contextMenuStrip_DataGrid;
            this.dataGrid.RowHeadersWidth = 62;
            this.dataGrid.Size = new System.Drawing.Size(772, 361);
            this.dataGrid.TabIndex = 6;
            this.dataGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_TaskLogList_CellValueChanged);
            this.dataGrid.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dataGridView_TaskLogList_UserDeletedRow);
            this.dataGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dataGridView_TaskLogList_UserDeletingRow);
            this.dataGrid.DoubleClick += new System.EventHandler(this.dataGridView_TaskLogList_DoubleClick);
            // 
            // StartDateTime
            // 
            this.StartDateTime.DataPropertyName = "StartDateTime";
            this.StartDateTime.Frozen = true;
            this.StartDateTime.HeaderText = "Date and Time";
            this.StartDateTime.MinimumWidth = 8;
            this.StartDateTime.Name = "StartDateTime";
            this.StartDateTime.Width = 160;
            // 
            // Pair
            // 
            this.Pair.Frozen = true;
            this.Pair.HeaderText = "Pair";
            this.Pair.MinimumWidth = 8;
            this.Pair.Name = "Pair";
            this.Pair.ReadOnly = true;
            this.Pair.Width = 150;
            // 
            // Timeframe
            // 
            this.Timeframe.Frozen = true;
            this.Timeframe.HeaderText = "Timeframe";
            this.Timeframe.MinimumWidth = 8;
            this.Timeframe.Name = "Timeframe";
            this.Timeframe.ReadOnly = true;
            this.Timeframe.Width = 150;
            // 
            // LogWindow
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(788, 400);
            this.Controls.Add(this.dataGrid);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(680, 306);
            this.Name = "LogWindow";
            this.Text = "Alerts";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LogWindow_FormClosing);
            this.Load += new System.EventHandler(this.LogWindow_Load);
            this.contextMenuStrip_SysTrayContext.ResumeLayout(false);
            this.contextMenuStrip_DataGrid.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        private NotifyIcon notifyIcon1;
        private Timer timer_StatusUpdate;
        private Timer timer_NotifySingleClick;
        private FrameworkClassReplacements.DataGridViewWithRowContextMenu dataGrid;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem deleteLogToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ContextMenuStrip contextMenuStrip_SysTrayContext;
        private ToolStripMenuItem openLogWindowToolStripMenuItem;
        private ToolStripMenuItem startTaskToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem1;
        private ContextMenuStrip contextMenuStrip_DataGrid;
        private ToolStripMenuItem updateTaskToolStripMenuItem;
        private ToolStripMenuItem splitTaskToolStripMenuItem;
        private ToolStripMenuItem deleteTaskToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private DataGridViewTextBoxColumn StartDateTime;
        private DataGridViewTextBoxColumn Pair;
        private DataGridViewTextBoxColumn Timeframe;
    }
}

