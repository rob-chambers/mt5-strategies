using System;
using System.Drawing;
using System.Windows.Forms;
using ManagedWinapi;

namespace cTrader.DesktopNotifications
{
    public partial class LogWindow : Form
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        #region Local Variables

        //State
        private bool _displayingDialog;
        private bool _taskInProgress;
        private DateTime _taskInProgressStartTime;
        private string _taskInProgressDescription = "";
        private string _taskInProgressCategory = "";
        private bool _taskInProgressTimeBillable = true;

        // TEMP
        bool _exiting = false;
        BindingSource _dataSet1BindingSource;

        //Resources
        private Icon TaskInProgressIcon;
        private Icon NoTaskActiveIcon;
        private Hotkey TaskHotKey;
        private DatabaseManager _databaseManager;

        #endregion

        #region Event Handlers

        private void LogWindow_Load(object sender, EventArgs e)
        {
            TaskInProgressIcon = new Icon(typeof(Icons.IconTypePlaceholder), "view-calendar-tasks-combined.ico");
            NoTaskActiveIcon = new Icon(typeof(Icons.IconTypePlaceholder), "edit-clear-history-2-combined.ico");

            TaskHotKey = new Hotkey
            {
                WindowsKey = true,
                Ctrl = true,
                KeyCode = System.Windows.Forms.Keys.T
            };
            TaskHotKey.HotkeyPressed += new EventHandler(TaskHotKey_HotkeyPressed);
            try
            {
                TaskHotKey.Enabled = true;
            }
            catch (HotkeyAlreadyInUseException)
            {
                //TODO: Make this an option, and simply disable if fail on start/load.
                // -> any error to be displayed should be displayed in real-time on options screen, in a special popup.
                // -> use ManagedWinapi.ShortcutBox
            }

            _databaseManager = new DatabaseManager();
            _databaseManager.LoadDatabase();
            _databaseManager.ReadAutoCompletionDataFromDB();
            _dataSet1BindingSource = _databaseManager.GetBindingSource();
            dataGrid.DataSource = _dataSet1BindingSource;

            UpdateControlDisplayConsistency(); //set initial display filter, etc (will be called again in updatestate)
            UpdateStateFromData(true);
        }

        private void LogWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_exiting)
            {
                //behave like Windows Messenger and other systray-based programs, hijack exit for close and explain
                e.Cancel = true;
                LogWindowCloseWithWarning();
            }
            else
            {
                if (UIUtils.DismissableWarning("Exiting Application", "You are exiting the Nano TimeTracker application - to later track time later you will need to start it again. If you just want to hide the window, then cancel here and choose \"Close\" instead.", "HideExitWarning"))
                {
                    if (_taskInProgress)
                    {
                        if (!UIUtils.DismissableWarning("Exiting - Task In Progress", "You are exiting Nano TimeTracker, but you still have a task in progress. The next time you start the application, you will be asked to confirm when that task completed.", "HideExitTaskInProgressWarning"))
                        {
                            //user cancelled on the exit task in progress warning
                            e.Cancel = true;
                        }
                    }
                }
                else
                {
                    //user cancelled on the exit warning
                    e.Cancel = true;
                }
            }

            // assuming we didn't actually exit, reset exiting flag for next time
            if (e.Cancel)
                _exiting = false;
            else
                TaskHotKey.Dispose();
        }

        private void btn_Stop_Click(object sender, System.EventArgs e)
        {
            PromptTask();
        }

        private void btn_Start_Click(object sender, System.EventArgs e)
        {
            PromptTask();
        }

        void TaskHotKey_HotkeyPressed(object sender, EventArgs e)
        {
            PromptTask();
        }

        private void timer_StatusUpdate_Tick(object sender, System.EventArgs e)
        {
            UpdateStatusDisplay();
        }

        private void notifyIcon1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) && (e.Clicks == 1) && (timer_NotifySingleClick.Enabled == false))
                timer_NotifySingleClick.Start();
        }

        private void notifyIcon1_DoubleClick(object sender, System.EventArgs e)
        {
            ToggleLogWindowDisplay();
        }

        private void timer_NotifySingleClick_Tick(object sender, System.EventArgs e)
        {
            timer_NotifySingleClick.Stop();
            PromptTask();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogWindowCloseWithWarning();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _exiting = true;
            Close();
        }

        private void deleteLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string caption = "Confirm Delete";
            DialogResult result;

            result = MessageBox.Show(this, "Are you SURE you want to delete the logfile?", caption, MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                result = MessageBox.Show(this, "Really Sure?", caption, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    if (_taskInProgress)
                    {
                        PromptTask();
                        //TODO: handle cancellation
                    }
                    _databaseManager.DeleteLogs();
                }

            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: IMPLEMENT OPTIONS FORM 

            string messageText;
            messageText = "Current LogFile Path: \u000D\u000A" + Properties.Settings.Default.LogFilePath;
            messageText = messageText + "\u000D\u000A\u000D\u000A" + "To change these options, please use regedit (or something like that).";

            MessageBox.Show(messageText, "LogFile Path", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void dataGridView_TaskLogList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) //to avoid strange calls during initialize
            {
                if (dataGrid.Columns[e.ColumnIndex].Name == "StartDateTime"
                    || dataGrid.Columns[e.ColumnIndex].Name == "EndDateTime"
                    || dataGrid.Columns[e.ColumnIndex].Name == "TimeTaken"
                    || dataGrid.Columns[e.ColumnIndex].Name == "BillableFlag"
                    )
                {
                    //looks like we should consider it, call the function that does the work.
                    MaintainDurationByDataGrid(e.RowIndex);
                }

                if (dataGrid.Columns[e.ColumnIndex].Name == "TaskName"
                    || dataGrid.Columns[e.ColumnIndex].Name == "TaskCategory"
                    || dataGrid.Columns[e.ColumnIndex].Name == "BillableFlag"
                    )
                {
                    _databaseManager.AutoCompletionCache.Feed((string)dataGrid.Rows[e.RowIndex].Cells["TaskName"].Value,
                        (string)dataGrid.Rows[e.RowIndex].Cells["TaskCategory"].Value,
                        (bool)dataGrid.Rows[e.RowIndex].Cells["BillableFlag"].Value);
                }


                if (false)
                {
                    //TODO: throw validation errors if you're trying to do somethng illegal, like delete end date?
                }

                //dirty hack to flush value to dataset (alternative method was to set currentcell to a cell in a 
                //different row, but was horrible hack and failed when only one row existed!). Found this cleaner 
                //suggestion on StackOverflow:
                // http://stackoverflow.com/questions/963601/datagridview-value-does-not-gets-saved-if-selection-is-not-lost-from-a-cell
                dataGrid.CurrentCell = null;
                dataGrid.CurrentCell = dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];

                _databaseManager.SaveTimeTrackingDB();
                UpdateStateFromData(false);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var box = new Dialogs.AboutBox())
            {
                box.ShowDialog();
            }    
        }

        private void openLogWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleLogWindowDisplay();
        }

        private void startTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PromptTask();
        }

        private void stopEditTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PromptTask();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _exiting = true;
            Close();
        }

        private void dataGridView_TaskLogList_DoubleClick(object sender, EventArgs e)
        {
            //TODO: clean up right double-click handling (right double-click doesn't mean anything normally right?)

            //first select the row whose cell was double-clicked (if single cell)
            if (dataGrid.SelectedCells.Count == 1 && !dataGrid.SelectedCells[0].OwningRow.IsNewRow && !dataGrid.SelectedCells[0].IsInEditMode)
                dataGrid.Rows[dataGrid.SelectedCells[0].RowIndex].Selected = true;

            //then display the edit dialog (if a single row is selected)
            if (dataGrid.SelectedRows.Count == 1 && !dataGrid.SelectedRows[0].IsNewRow)
                PromptTask((DateTime)dataGrid.SelectedRows[0].Cells[0].Value);
        }


        private void updateTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGrid.SelectedRows.Count == 1)
                PromptTask((DateTime)dataGrid.SelectedRows[0].Cells[0].Value);
        }

        private void deleteTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGrid.SelectedRows.Count == 1)
            {
                if (MessageBox.Show("Are you sure you want to delete this task entry?", "Really Delete?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    dataGrid.Rows.Remove(dataGrid.SelectedRows[0]);
                    //this doesn't fire "UserDeletedRow" event, so call persistence & state updates manually
                    _databaseManager.SaveTimeTrackingDB();
                    UpdateStateFromData(true);
                }
            }
        }

        private void dataGridView_TaskLogList_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this task entry?", "Really Delete?", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void dataGridView_TaskLogList_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            _databaseManager.SaveTimeTrackingDB();
            UpdateStateFromData(true);
        }

        private void dateNavigator1_DateValueChanged()
        {
            UpdateControlDisplayConsistency();
        }

        #endregion

        #region Local Methods

        private void LogWindowCloseWithWarning()
        {
            if (UIUtils.DismissableWarning("Closing Window", "The Nano TimeTracker log window is closing, but the program will continue to run - you can start and stop tasks, or open the main window, by clicking on the icon in the tray on the bottom right.", "HideCloseWarning"))
                this.Hide();
        }

        private void UpdateControlDisplayConsistency()
        {
            // "Open Log Window" context strip option display
            if (Visible && openLogWindowToolStripMenuItem.Enabled)
                openLogWindowToolStripMenuItem.Enabled = false;

            if (!Visible && !_displayingDialog && !openLogWindowToolStripMenuItem.Enabled)
                openLogWindowToolStripMenuItem.Enabled = true;

            if (_displayingDialog && openLogWindowToolStripMenuItem.Enabled)
                openLogWindowToolStripMenuItem.Enabled = false;

            // systray context strip
            if (_taskInProgress && startTaskToolStripMenuItem.Enabled)
                startTaskToolStripMenuItem.Enabled = false;
            else if (!_taskInProgress && !startTaskToolStripMenuItem.Enabled)
                startTaskToolStripMenuItem.Enabled = true;

            // icons
            if (_taskInProgress && notifyIcon1.Icon != TaskInProgressIcon)
                notifyIcon1.Icon = TaskInProgressIcon;
            else if (!_taskInProgress && notifyIcon1.Icon != NoTaskActiveIcon)
                notifyIcon1.Icon = NoTaskActiveIcon;

            if (_taskInProgress && this.Icon != TaskInProgressIcon)
                this.Icon = TaskInProgressIcon;
            else if (!_taskInProgress && this.Icon != NoTaskActiveIcon)
                this.Icon = NoTaskActiveIcon;

            // SysTray Balloon
            if (!_taskInProgress && !notifyIcon1.Text.Equals("Nano TimeLogger - no active task"))
                notifyIcon1.Text = "Nano TimeLogger - no active task";
        }

        private void ToggleLogWindowDisplay()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;
            }
        }

        private void PromptTask()
        {
            PromptTask(null);
        }

        private void PromptTask(DateTime? existingTaskTime)
        {
            //double-check whether we really should be displaying a dialog at all
            if (_displayingDialog)
            {
                return;
            }

            _displayingDialog = true;
            UpdateControlDisplayConsistency();

            //save and switch day if appropriate, before starting any new logging
            if (!_taskInProgress && existingTaskTime == null)
                _databaseManager.SaveTimeTrackingDB(true);

            bool? doAction = null;
            //only relevant if we're not minimized, but seems to do no harm.
            this.Activate();

            string promptMessage;
            string promptTitle;
            DateTime promptStartDate;
            DateTime? promptEndDate;
            string promptDescription;
            string promptCategory;
            bool promptTimeBillable;

            if (existingTaskTime != null)
            {
                //we are updating a SPECIFIC task
                promptMessage = "Please provide updated Task details:";
                promptTitle = "Update Task";
                promptStartDate = existingTaskTime.Value;
                if (!_databaseManager.GetTaskDetailsByTask(existingTaskTime.Value, out promptEndDate, out promptDescription, out promptCategory, out promptTimeBillable))
                {
                    MessageBox.Show("Failed to retrieve task details!");
                    doAction = false;
                }
            }
            else
            {
                //we are updating the CURRENT task (or starting a new one)
                promptDescription = _taskInProgressDescription;
                promptCategory = _taskInProgressCategory;
                promptTimeBillable = _taskInProgressTimeBillable;

                if (!_taskInProgress)
                {
                    promptMessage = "New task details:";
                    promptTitle = "Task Entry - New Task";
                    promptStartDate = DateTime.Now;
                    promptEndDate = null;
                }
                else
                {
                    promptMessage = "Task details:";
                    promptTitle = "Task Entry - Confirm Task Details / End Task";
                    promptStartDate = _taskInProgressStartTime;
                    promptEndDate = DateTime.Now;
                }
            }

            DateTime providedStartDate = DateTime.Now;
            DateTime? providedEndDate = null;
            string providedDescription = "";
            string providedCategory = "";
            bool providedTimeBillable = false;

            if (doAction == null)
            {
            }

            if (doAction.Value)
            {
                if (existingTaskTime != null)
                {
                    //update the SPECIFIC entry that was passed in
                    _databaseManager.UpdateLogTask(existingTaskTime.Value, providedStartDate, providedEndDate, providedDescription, providedCategory, providedTimeBillable);
                }
                else
                {
                    //update log if already running but we're not completing
                    if (_taskInProgress && providedEndDate == null)
                        _databaseManager.UpdateLogOpenTask(providedStartDate, providedEndDate, providedDescription, providedCategory, providedTimeBillable);

                    //start log if not already in-progress
                    if (!_taskInProgress)
                    {
                        _databaseManager.StartLoggingTask(providedStartDate, providedDescription, providedCategory, providedTimeBillable);
                    }

                    //end log if end date provided
                    if (providedEndDate != null)
                    {
                        _databaseManager.UpdateLogOpenTask(providedStartDate, providedEndDate, providedDescription, providedCategory, providedTimeBillable);
                    }
                }

                UpdateStateFromData(true);
            }

            //release dialog "lock" and update display
            _displayingDialog = false;
            UpdateControlDisplayConsistency();
        }

        private void UpdateStatusDisplay()
        {
            System.TimeSpan timeSinceTaskStart;
            if (_taskInProgress)
            {
                timeSinceTaskStart = DateTime.Now.Subtract(_taskInProgressStartTime);
                notifyIcon1.Text = "Time Logger - " + Utils.FormatTimeSpan(timeSinceTaskStart);
            }
            else
            {
                timeSinceTaskStart = new TimeSpan();
                notifyIcon1.Text = "Time Logger";
            }            
        }

        private void MaintainDurationByDataGrid(int RowIndex)
        {
            DateTime from;
            DateTime to;
            if (dataGrid.Rows[RowIndex].Cells["StartDateTime"].Value.ToString() != "")
                from = (System.DateTime)(System.DateTime)dataGrid.Rows[RowIndex].Cells["StartDateTime"].Value;
            else
                from = System.DateTime.MinValue;
            if (dataGrid.Rows[RowIndex].Cells["EndDateTime"].Value.ToString() != "")
                to = (System.DateTime)dataGrid.Rows[RowIndex].Cells["EndDateTime"].Value;
            else
                to = System.DateTime.MinValue;
            if (from != System.DateTime.MinValue && to != System.DateTime.MinValue)
            {
                dataGrid.Rows[RowIndex].Cells["TimeTaken"].Value = to.Subtract(from).TotalHours;
            }
            else
            {
                dataGrid.Rows[RowIndex].Cells["TimeTaken"].Value = DBNull.Value;
            }
        }

        private void UpdateStateFromData(bool allowReFocus)
        {
            DateTime existingTaskStartTime;
            string existingTaskDescription;
            string existingTaskCategory;
            bool existingTaskBillable;

            if (_databaseManager.GetInProgressTaskDetails(out existingTaskStartTime, out existingTaskDescription, out existingTaskCategory, out existingTaskBillable))
            {
                _taskInProgress = true;
                _taskInProgressStartTime = existingTaskStartTime;
                _taskInProgressDescription = existingTaskDescription;
                _taskInProgressCategory = existingTaskCategory;
                _taskInProgressTimeBillable = existingTaskBillable;
            }
            else
            {
                _taskInProgress = false;
            }

            //retrieve existing totals for display
            //_todayTotalHours = _databaseManager.GetHoursTotals(DateTime.Today, DateTime.Today, false);
            //_todayBillableHours = _databaseManager.GetHoursTotals(DateTime.Today, DateTime.Today, true);
            //_thisWeekBillableHours = _databaseManager.GetHoursTotals(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today, true);
            //_thisMonthBillableHours = _databaseManager.GetHoursTotals(DateTime.Today.AddDays(1-DateTime.Today.Day), DateTime.Today, true);

            //UI updates specific to task running change
            UpdateControlDisplayConsistency();
            if (allowReFocus)
            {
                if (_taskInProgress)
                {
                    if (dataGrid.Rows.Count > 0)
                        dataGrid.CurrentCell = dataGrid.Rows[dataGrid.Rows.Count - 1].Cells[0];
                }
            }

            UpdateStatusDisplay();
        }

        #endregion

    }
}
