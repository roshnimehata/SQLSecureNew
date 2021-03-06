using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Idera.SQLsecure.Core.Accounts;
using Idera.SQLsecure.Core.Logger;
using Idera.SQLsecure.UI.Console.Controls;
using Idera.SQLsecure.UI.Console.Sql;
using Idera.SQLsecure.UI.Console.Utility;
using Infragistics.Win.UltraWinListView;
using Policy = Idera.SQLsecure.UI.Console.Sql.Policy;
using Idera.SQLsecure.UI.Console.SQL;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Idera.SQLsecure.UI.Console.Forms
{
    public partial class Form_WizardRegisterSQLServer : Form
    {
        #region Constants
        private const string WizardIntroText = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fswiss\fcharset0 Microsoft Sans Serif;}{\f1\fnil\fcharset2 Symbol;}}
{\*\generator Msftedit 5.41.15.1507;}\viewkind4\uc1\pard\f0\fs16 This wizard allows you to add a SQL Server to SQLsecure for auditing.  With this wizard you will:\par
\par
\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent360{\pntxtb\'B7}}\fi-360\li720\tx720 Select a SQL Server\par
{\pntext\f1\'B7\tab}Specify the credentials used to connect to the server\par
{\pntext\f1\'B7\tab}Select which SQL Server objects to audit\par
{\pntext\f1\'B7\tab}Specify the data collection schedule\par
{\pntext\f1\'B7\tab}Specify the Email Notification for data collections\par
{\pntext\f1\'B7\tab}Select which Policies will include this SQL Server\fs18\par
}";

        private const string WizardFinishTextPrefix = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fswiss\fprq2\fcharset0 Microsoft Sans Serif;}{\f1\fswiss\fcharset0 Arial;}}
{\*\generator Msftedit 5.41.15.1507;}\viewkind4\uc1\pard\nowidctlpar\fi-1584\li1584\tx1440\f0\fs16";
        private const string WizardFinishTextSQLServer = @"\b SQL Server\b0\tab :  ";
        private const string WizardFinishTextCredentials = @"\par\b Credentials\b0\tab :  ";
        private const string WizardFinishTextFilters = @"\par\b Collection Filter\b0\tab :  ";
        private const string WizardFinishTextSchedule = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fswiss\fprq2\fcharset0 Microsoft Sans Serif;}{\f1\fswiss\fcharset0 Arial;}}
{\*\generator Msftedit 5.41.15.1507;}\viewkind4\uc1\pard\nowidctlpar\fi-1584\li1584\tx1440\f0\fs16
\par\b Job Schedule\b0\tab :  ";
        private const string WizardFinishTextEmailNotifications = @"\par\b Notification\b0\tab :  ";
        private const string WizardFinishTextPolicies = @"\par\b Policies\b0\tab :  ";
        private const string WizardFinishTextCollectData = @"\par\b Data Collection\b0\tab :  ";

        private const string WizardFinishTextSuffix = @"\par\pard\tx720\par
\pard\f1 As part of the SQL Server registration process, a data collection job will be created. \f2\fs20\par
}";
        #endregion

        #region Fields

        private static LogX logX = new LogX("Idera.SQLsecure.UI.Console.Forms.Form_WizardRegisterSQLServer");
        private string m_Machine = string.Empty;
        private string m_Instance = string.Empty;
        private string m_Connection = string.Empty;
        private int? m_ConnectionPort = null;
        private string m_Version = string.Empty;
        private string m_ServerType = string.Empty;
        private Sql.DataCollectionFilter m_Filter;
        private List<Console.Sql.Policy> m_Polices = new List<Console.Sql.Policy>();
        private NextAction m_nextAction;
        private ScheduleJob.ScheduleData m_scheduleData;
        private bool needToConfigureSMTPProvider;
        private bool needToConfigureEmail;
        private bool hasSelectablePolicy;
        private List<Tag> Tags = new List<Tag>();
        enum NextAction
        {
            RunCollection,
            LaunchProperties,
            Nothing
        }

        #endregion

        #region Ctors

        public Form_WizardRegisterSQLServer()
        {
            InitializeComponent();
            hasSelectablePolicy = false;
            m_nextAction = NextAction.Nothing;

            // Set the intro text in the wizard.
            _rtb_Introduction.Rtf = WizardIntroText;

            // Select the intro page.
            _wizard.SelectedPage = _page_Introduction;

            // Default to Windows Authentication for SQL login
            radioButton_WindowsAuth.Checked = true;
            textbox_SqlLogin.Enabled = false;
            textbox_SqlLoginPassword.Enabled = false;

            // Default to use same as Windows Auth to false
            checkBox_UseSameAuth.Checked = true;
            textbox_WindowsUser.Enabled = false;
            textbox_WindowsPassword.Enabled = false;


            _comboBox_ServerType.Items.Clear();
            _comboBox_ServerType.Items.Add(Utility.Activity.TypeServerOnPremise);
            _comboBox_ServerType.Items.Add(Utility.Activity.TypeServerAzureVM);
            _comboBox_ServerType.Items.Add(Utility.Activity.TypeServerAzureDB);
            _comboBox_ServerType.SelectedItem = Utility.Activity.TypeServerOnPremise;
            _comboBox_ServerType.SelectedIndexChanged +=
            new System.EventHandler(_comboBox_ServerType_SelectedIndexChanged);
            // Load up the Polices
            foreach (Policy p in Program.gController.Repository.Policies)
            {
                if (!p.IsDynamic)
                {
                    UltraListViewItem li = ultraListView_Policies.Items.Add(null, p.PolicyName);
                    li.Tag = p;
                    hasSelectablePolicy = true;
                }
                else
                {
                    UltraListViewItem li2 = ultraListView_DynamicPolicies.Items.Add(null, p.PolicyName);
                    li2.Tag = p;
                }
            }

            // Default to notify on Error
            checkBoxEmailForCollectionStatus.Checked = true;
            radioButton_SendEmailOnError.Checked = true;

            checkBoxEmailFindings.Checked = true;
            radioButtonSendEmailFindingAny.Checked = true;

            // Default to notfiy on Vulnerablity Moderate
            radioButtonSendEmailFindingHigh.Checked = true;

            controlSMTPEmailConfig1.RegisterDataOKDelegate(SMTPDataEntered);
            controlSMTPEmailConfig1.InitializeControl(Program.gController.Repository.NotificationProvider);
            needToConfigureSMTPProvider = !Program.gController.Repository.NotificationProvider.IsValid();
            needToConfigureEmail = string.IsNullOrEmpty(Program.gController.Repository.NotificationProvider.ServerName);

            // Set defaults for scheduled data
            // Every sunday at 3am, keep data for 60 days
            // ------------------------------------------
            m_scheduleData.SetDefaults();

            numericUpDown_KeepSnapshotDays.Value = m_scheduleData.snapshotretentionPeriod;
            _btn_ChangeSchedule.Enabled = m_scheduleData.Enabled;
            checkBox_EnableScheduling.Checked = m_scheduleData.Enabled;
            ScheduleJob.BuildDescription(ref m_scheduleData);
            _txtbx_ScheduleDescription.Text = m_scheduleData.Description;
            if (Sql.ScheduleJob.IsSQLAgentStarted(Program.gController.Repository.ConnectionString))
            {
                label_AgentStatus.Text = Utility.ErrorMsgs.SQLServerAgentStarted;
                pictureBox_AgentStatus.Image = AppIcons.AppImage48(AppIcons.EnumImageList48.AgentStarted);
            }
            else
            {
                label_AgentStatus.Text = Utility.ErrorMsgs.SQLServerAgentStopped;
                pictureBox_AgentStatus.Image = AppIcons.AppImage48(AppIcons.EnumImageList48.AgentStopped);
            }
        }

        #endregion

        #region Helpers

        private static bool checkVersionAndRegistration(
                string version,
                string connection
            )
        {
            Debug.Assert(!string.IsNullOrEmpty(version));

            // Check the version.
            bool isOk = true;
            if (Sql.SqlHelper.ParseVersion(version) == Sql.ServerVersion.Unsupported)
            {
                Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.ServerVersionNotSupportedMsg);
                isOk = false;
            }

            // Check if the connection is already registered in SQLsecure.
            if (isOk)
            {
                try
                {
                    isOk = !Sql.RegisteredServer.IsServerRegistered(Program.gController.Repository.ConnectionString, connection);
                    if (!isOk)
                    {
                        Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.ServerAlreadyRegisteredMsg);
                    }
                }
                catch (Exception ex)
                {
                    isOk = false;
                    Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.ServerRegistrationCheckFailedMsg, ex);
                }
            }

            return isOk;
        }

        #endregion

        #region Methods

        public static void Process()
        {
            // Do we have any available licenses for a new server
            if (!Program.gController.Repository.bbsProductLicense.IsLicneseGoodForServerCount(Program.gController.Repository.RegisteredServers.Count + 1))
            {
                Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.RegisterSqlServerNoLicenseMsg);
                return;
            }

            // Display the wizard.
            Form_WizardRegisterSQLServer form = new Form_WizardRegisterSQLServer();
            DialogResult rc = form.ShowDialog();

            // Process if user hit finish.
            if (rc == DialogResult.OK)
            {
                // Create an entry for the connection with the specified
                // parameters in the repository.
                bool isOk = true;
                int rID = 0;
                try
                {
                    string sqlLogin = form.textbox_SqlLogin.Text;
                    string sqlPassword = form.textbox_SqlLoginPassword.Text;
                    if (form.radioButton_WindowsAuth.Checked)
                    {
                        sqlLogin = form.textBox_SQLWindowsUser.Text;
                        sqlPassword = form.textBox_SQLWindowsPassword.Text;
                    }

                    string[] auditFolders = form.addEditFoldersControl.GetFolders();
                    Sql.RegisteredServer.AddServer(Program.gController.Repository.ConnectionString,
                                        form.m_Connection, form.m_ConnectionPort, form.m_Machine, form.m_Instance,
                                        form.radioButton_WindowsAuth.Checked ? "W" : "S",
                                        sqlLogin, sqlPassword,
                                        form.textbox_WindowsUser.Text, form.textbox_WindowsPassword.Text,
                                        form.m_Version, (int)form.numericUpDown_KeepSnapshotDays.Value, auditFolders,form.m_ServerType);

                    // Notify controller that a new server was added.
                    Program.gController.SignalRefreshServersEvent(true, form._txtbx_Server.Text.ToUpper());
                }
                catch (Exception ex)
                {
                    isOk = false;
                    Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.AddServerToRepositoryFailedMsg, ex);
                }

                // Add Server to requested Policies
                if (isOk)
                {
                    rID = Sql.RegisteredServer.GetServer(form.m_Connection).RegisteredServerId;
                    foreach (Policy p in form.m_Polices)
                    {
                        Sql.RegisteredServer.AddRegisteredServerToPolicy(rID, p.PolicyId, p.AssessmentId);
                    }
                }

                // Add Email Notification
                if (isOk)
                {
                    if (form.needToConfigureSMTPProvider)
                    {
                        form.controlSMTPEmailConfig1.GetNotificationProvider().AddNotificationProvider(Program.gController.Repository.ConnectionString);
                        Program.gController.Repository.RefreshNotificationProvider();
                    }
                    int providerId = Program.gController.Repository.NotificationProvider.ProviderId;
                    Idera.SQLsecure.Core.Accounts.RegisteredServerNotification rSN = new RegisteredServerNotification(Utility.SQLCommandTimeout.GetSQLCommandTimeoutFromRegistry());
                    rSN.RegisteredServerId = rID;
                    rSN.ProviderId = providerId;
                    rSN.Recipients = form.textBox_Recipient.Text;
                    if (form.checkBoxEmailForCollectionStatus.Checked)
                    {
                        if (form.radioButtonAlways.Checked)
                        {
                            rSN.SnapshotStatus = RegisteredServerNotification.SnapshotStatusNotification_Always;
                        }
                        else if (form.radioButton_SendEmailWarningOrError.Checked)
                        {
                            rSN.SnapshotStatus = RegisteredServerNotification.SnapshotStatusNotification_OnWarningOrError;
                        }
                        else if (form.radioButton_SendEmailOnError.Checked)
                        {
                            rSN.SnapshotStatus = RegisteredServerNotification.SnapshotStatusNotification_OnlyOnError;
                        }
                    }
                    else
                    {
                        rSN.SnapshotStatus = RegisteredServerNotification.SnapshotStatusNotification_Never;
                    }
                    if (form.checkBoxEmailFindings.Checked)
                    {
                        if (form.radioButtonSendEmailFindingAny.Checked)
                        {
                            rSN.PolicyMetricSeverity = (int)RegisteredServerNotification.MetricSeverityNotificaiton.Any;
                        }
                        else if (form.radioButtonSendEmailFindingHighMedium.Checked)
                        {
                            rSN.PolicyMetricSeverity = (int)RegisteredServerNotification.MetricSeverityNotificaiton.OnWarningAndError;
                        }
                        else if (form.radioButtonSendEmailFindingHigh.Checked)
                        {
                            rSN.PolicyMetricSeverity = (int)RegisteredServerNotification.MetricSeverityNotificaiton.OnlyOnError;
                        }
                    }
                    else
                    {
                        rSN.PolicyMetricSeverity = (int)RegisteredServerNotification.MetricSeverityNotificaiton.Never;
                    }
                    rSN.AddNotificationProvider(Program.gController.Repository.ConnectionString);
                }

                // Add rules to the repository.
                if (isOk)
                {
                    try
                    {
                        form.m_Filter.CreateFilter(Program.gController.Repository.ConnectionString, form.m_Connection);
                    }
                    catch (Exception ex)
                    {
                        isOk = false;
                        Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.AddRuleToRepositoryFailedMsg, ex);
                    }
                }

                // Add job to repository
                if (isOk)
                {
                    try
                    {
                        Guid jobID = Sql.ScheduleJob.AddJob(Program.gController.Repository.ConnectionString,
                                              form.m_Connection,
                                              Program.gController.Repository.Instance,
                                              form.m_scheduleData);

                        // Update Registered Server with new jobID 
                        Program.gController.Repository.GetServer(form.m_Connection).SetJobId(jobID);

                        if (form.m_nextAction == NextAction.RunCollection)
                        {
                            Form_StartSnapshotJobAndShowProgress.Process(form.m_Connection, jobID);
                        }
                        else if (form.m_nextAction == NextAction.LaunchProperties)
                        {
                            Form_SqlServerProperties.Process(form.m_Connection, Forms.Form_SqlServerProperties.RequestedOperation.EditCofiguration, Program.gController.isAdmin);
                        }
                        else
                        {
                            //// Prompt to see if user wants to run a collection job now
                            //string msg =
                            //    string.Format(Utility.ErrorMsgs.RegisterSqlServerSuccessPromptToRunJob, form.m_Instance);
                            //DialogResult dr =
                            //    Utility.MsgBox.ShowQuestion(Utility.ErrorMsgs.RegisterSqlServerCaption,
                            //                                msg);
                            //if (dr == System.Windows.Forms.DialogResult.Yes)
                            //{
                            //    Form_StartSnapshotJobAndShowProgress.Process(form.m_Connection, jobID);
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        isOk = false;
                        Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.AddJobToRepositoryFailedMsg, ex);
                    }
                }
                //tags
                if (isOk)
                {
                    try
                    {
                        List<string> tagsIds = new List<string>();
                        foreach (var item in form.ulTags.CheckedItems)
                        {
                            var tagId = item.Tag.ToString();
                            tagsIds.Add(tagId);
                        }
                        TagWorker.AssignServerToTags(rID, tagsIds);
                    }
                    catch (Exception ex)
                    {
                        isOk = false;
                        Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, Utility.ErrorMsgs.AddJobToRepositoryFailedMsg, ex);
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void _wizard_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            string helpTopic = Utility.Help.RegisterSQLServerWizardHelpTopic;
            if (_page_Introduction.Visible)
                helpTopic = Utility.Help.RegisterSQLServerWizardHelpTopic;
            else if (_page_Servers.Visible)
                helpTopic = Utility.Help.AddServerGeneralHelpTopic;
            else if (_PageTags.Visible)
                helpTopic = Utility.Help.ManageTagsHelpTopic;
            else if (_page_Credentials.Visible)
                helpTopic = Utility.Help.AddServerCredentialsHelpTopic;
            else if (_page_FilePermissionFolders.Visible)
                helpTopic = Utility.Help.ServerAuditFoldersHelpTopic;
            else if (_page_JobSchedule.Visible)
                helpTopic = Utility.Help.AddServerScheduleHelpTopic;
            else if (_page_DefineFilters.Visible)
                helpTopic = Utility.Help.AddServerFiltersHelpTopic;
            else if (_page_NotificationOptions.Visible)
                helpTopic = Utility.Help.AddServerEmailHelpTopic;
            else if (_page_Policies.Visible)
                helpTopic = Utility.Help.AddServerPoliciesHelpTopic;
            else if (_page_CollectData.Visible)
                helpTopic = Utility.Help.AddServerCollectionTopic;
            else if (_page_Finish.Visible)
                helpTopic = Utility.Help.AddServerReviewHelpTopic;

            Program.gController.ShowTopic(helpTopic);
        }

        private void _comboBox_ServerType_SelectedIndexChanged(object sender,
        System.EventArgs e)
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_WizardRegisterSQLServer));
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem == Utility.Activity.TypeServerOnPremise)
            {
                _btn_BrowseServers.Visible = true;
                // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                _lbl_AzureAuth.Hide();
                checkBox_UseSameAuth.Enabled = true;
                checkBox_UseSameAuth.Checked = true;
                checkBox_UseSameAuth.Show();
                radioButton_SQLServerAuth.Checked = true;
                _grpbx_WindowsGMCredentials.Visible = true;
                checkBox_UseSameAuth.Text = "Use same Windows Authentication as above";
                radioButton_WindowsAuth.Text = "Windows Authentication";
                label4.Text = "&Windows User:";
                _lbl_WindowsUser.Text = "&Windows User:";
                _grpbx_WindowsGMCredentials.Text = "Windows Credentials to gather Operating System and Active Directory objects";
                label2.Text = resources.GetString("label2.Text");
            }
			// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
            else if (comboBox.SelectedItem == Utility.Activity.TypeServerAzureVM)
            {
                _btn_BrowseServers.Visible = false;//SQLsecure 3.1 (Tushar)--Fix for defect SQLSECU-1714
                checkBox_UseSameAuth.Enabled = false;
                checkBox_UseSameAuth.Checked = false;
                checkBox_UseSameAuth.Hide();
                _lbl_AzureAuth.Show();

                radioButton_WindowsAuth.Text = "Windows Authentication";
                label4.Text = "&Windows User:";
                _lbl_WindowsUser.Text = "&Azure AD Account:";
                radioButton_SQLServerAuth.Checked = true;

                textBox_SQLWindowsUser.Enabled = false;
                textBox_SQLWindowsPassword.Enabled = false;

                textbox_WindowsUser.Enabled = true;
                textbox_WindowsPassword.Enabled = true;

                textbox_SqlLogin.Enabled = true;
                textbox_SqlLoginPassword.Enabled = true;
                _grpbx_WindowsGMCredentials.Visible = true;
                _grpbx_WindowsGMCredentials.Text = "Azure Active Directory to gather Operating System and Active Directory objects";
                label2.Text = resources.GetString("AzureVMLabel2.Text");
            }
            else if (comboBox.SelectedItem == Utility.Activity.TypeServerAzureDB)
            {
              
                _btn_BrowseServers.Visible = false;
				// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                _lbl_AzureAuth.Hide();
                checkBox_UseSameAuth.Show();
                checkBox_UseSameAuth.Enabled = true;
                radioButton_SQLServerAuth.Checked = true;
                
                checkBox_UseSameAuth.Text = "Use same Azure AD Authentication as above";
                radioButton_WindowsAuth.Text = "Azure Active Directory";
                label4.Text = "&Azure AD Account:";
                _lbl_WindowsUser.Text = "&Azure AD Account:";
                radioButton_SQLServerAuth.Checked = true;
                _grpbx_WindowsGMCredentials.Visible = false;
                _grpbx_WindowsGMCredentials.Text = "Azure Active Directory Credentials to gather Active Directory objects";
                label2.Text = resources.GetString("AzureDbLabel2.Text");
                this._page_Credentials.NextPage = this._PageTags;
                this._PageTags.PreviousPage = this._page_Credentials;
            }
            
        }
        #region Select Server Page

        private void _page_Servers_BeforeDisplay(object sender, EventArgs e)
        {
            // If no text in server box disable next button.
            _page_Servers.AllowMoveNext = !string.IsNullOrEmpty(_txtbx_Server.Text);
        }

        private void _btn_BrowseServers_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            Form_SelectServer dlg = new Form_SelectServer();

            try
            {
                if (dlg.LoadServers())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _txtbx_Server.Text = dlg.SelectedServer;
                    }
                }
            }
            catch (Exception ex)
            {
                logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantValidateRepository), ex);
                Utility.MsgBox.ShowError(Utility.ErrorMsgs.CantValidateRepository, ex);
            }
            this.Cursor = Cursors.Default;
        }



        private void _txtbx_Server_TextChanged(object sender, EventArgs e)
        {
            // Enable next button, if text length is not null.
            if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerOnPremise)
            {
                _page_Servers.AllowMoveNext = !string.IsNullOrEmpty(_txtbx_Server.Text);
            }
            else
            {

                try
                {
                    
                    if (!Regex.IsMatch(_txtbx_PortNumber.Text, @"^\d+$"))
                    {   
                        _txtbx_PortNumber.ForeColor = System.Drawing.Color.Red;
                        _page_Servers.AllowMoveNext = false;
                    }
                    else
                    {
                        _txtbx_PortNumber.ForeColor = System.Drawing.Color.Black;
                        _page_Servers.AllowMoveNext = !string.IsNullOrEmpty(_txtbx_Server.Text) && !string.IsNullOrEmpty(_txtbx_PortNumber.Text);
                    }
                }
                catch
                {
                    // If there is an error, display the text using the system colors.
                    _txtbx_PortNumber.ForeColor = SystemColors.ControlText;
                }
                
            }
        }

        private void _page_Servers_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            Debug.Assert(!string.IsNullOrEmpty(_txtbx_Server.Text));

            if (string.IsNullOrEmpty(_txtbx_Server.Text))
            {
                e.Cancel = true;
            }
            else
            {
                string[] str = _txtbx_Server.Text.ToUpper().Split(',');
                if (Program.gController.Repository.RegisteredServers.Find(str[0]) != null)
                {
                    MsgBox.ShowError(ErrorMsgs.RegisterSqlServerCaption,
                                string.Format("Server {0} is already registered in SQLsecure", str[0]));
                    e.Cancel = true;
                }
                try
                {
                    m_ConnectionPort = Convert.ToInt32(str[1]);
                }
                catch
                {
                    m_ConnectionPort = null;
                }
            }
           
            // We have to get the server properties (machine & instance) from the
            // specified SQL Server.   If agent service credentials are being used
            // for data collection, connect to the registered SQL Server and get
            // the properties.   If there is a failure in connecting to the 
            // SQL Server then the next page to should ask for credentials for
            // completing the registration.
            //bool isOk = true;
            //bool isLoginFailed = false;
            //string version = string.Empty, machine = string.Empty, instance = string.Empty, connection = string.Empty;
            //if (_rdbtn_AgentCredentials.Checked)
            //{
            //    // Get server properties and validate version & instance.
            //    try
            //    {
            //        Sql.SqlServer.GetSqlServerProperties(_txtbx_Server.Text, "", "", out version, out machine, out instance, out connection);
            //        isOk = checkVersionAndRegistration(version, connection);
            //        if (isOk)
            //        {
            //            m_Version = version;
            //            _page_Servers.NextPage = _page_DefineFilters;
            //            _page_DefineFilters.PreviousPage = _page_Servers;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        // If its a login exception, then rewire to ask the user
            //        // for credentials.   Else display the exception.
            //        // NOTE : for login exception, the isOk flag is not cleared.
            //        if (Utility.ExceptionHelper.IsSqlLoginFailed(ex))
            //        {
            //            isLoginFailed = true;
            //            _page_Servers.NextPage = _page_RegSqlCredentials;
            //            _page_RegSqlCredentials.PreviousPage = _page_Servers;
            //            _page_RegSqlCredentials.NextPage = _page_DefineFilters;
            //            _page_DefineFilters.PreviousPage = _page_RegSqlCredentials;
            //        }
            //        else
            //        {
            //            isOk = false;
            //            Utility.MsgBox.ShowError(Utility.ErrorMsgs.RegisterSqlServerCaption, string.Format(Utility.ErrorMsgs.RegisterSqlServerCantFindServer, _txtbx_Server.Text));
            //        }
            //    }
            //}

            //// If no failures update machine, instance & connection fields.
            //// Else cancel move to next page.
            //if (isOk)
            //{
            //    // If agent credentials are being used and there was no login error
            //    // update the machine, instance and connection fields.
            //    if (_rdbtn_AgentCredentials.Checked && !isLoginFailed)
            //    {
            //        m_Machine = machine;
            //        m_Instance = instance;
            //        m_Connection = connection;
            //    }
            //}
            //else
            //{
            //    e.Cancel = true;
            //}
        }

        #endregion

        #region Credentials Page

        private void checkBox_UseSameAuth_CheckedChanged(object sender, EventArgs e)
        {
			// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
            if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureVM)
            {
                return;
            }

                if (checkBox_UseSameAuth.Checked)
            {
                textbox_WindowsUser.Text = textBox_SQLWindowsUser.Text;
                textbox_WindowsPassword.Text = textBox_SQLWindowsPassword.Text;

                textbox_WindowsUser.Enabled = false;
                textbox_WindowsPassword.Enabled = false;
            }
            else
            {
                textbox_WindowsUser.Enabled = true;
                textbox_WindowsPassword.Enabled = true;
            }
            updateCredentialsPageMoveNext();
        }

        private void radioButton_WindowsAuth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_WindowsAuth.Checked)
            {
				// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                if (_comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM)
                {
                    checkBox_UseSameAuth.Enabled = true;
                }            

                textBox_SQLWindowsUser.Enabled = true;
                textBox_SQLWindowsPassword.Enabled = true;
                textbox_SqlLogin.Enabled = false;
                textbox_SqlLoginPassword.Enabled = false;
            }

            updateCredentialsPageMoveNext();
        }

        private void radioButton_SQLServerAuth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_SQLServerAuth.Checked)
            {
				// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                if (_comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM)
                {
                    checkBox_UseSameAuth.Enabled = false;
                    checkBox_UseSameAuth.Checked = false;
                }

                textBox_SQLWindowsUser.Enabled = false;
                textBox_SQLWindowsPassword.Enabled = false;
                textbox_SqlLogin.Enabled = true;
                textbox_SqlLoginPassword.Enabled = true;
            }
            updateCredentialsPageMoveNext();
        }

        private void updateCredentialsPageMoveNext()
        {
            bool allowMoveNext = true;

            if (radioButton_WindowsAuth.Checked)
            {
                allowMoveNext = !string.IsNullOrEmpty(textBox_SQLWindowsUser.Text)
                                && !string.IsNullOrEmpty(textBox_SQLWindowsPassword.Text);
            }

            if (allowMoveNext && radioButton_SQLServerAuth.Checked)
            {
                allowMoveNext = !string.IsNullOrEmpty(textbox_SqlLogin.Text)
                                && !string.IsNullOrEmpty(textbox_SqlLoginPassword.Text);
            }
            //if(allowMoveNext)
            //{
            //    allowMoveNext = !string.IsNullOrEmpty(textbox_WindowsUser.Text)
            //                    && !string.IsNullOrEmpty(textbox_WindowsPassword.Text);
            //}


            _page_Credentials.AllowMoveNext = allowMoveNext;
        }

        private void textBox_SQLWindowsUser_TextChanged(object sender, EventArgs e)
        {
			// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
            if (checkBox_UseSameAuth.Checked && (_comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM))
            {
                textbox_WindowsUser.Text = textBox_SQLWindowsUser.Text;
            }
            updateCredentialsPageMoveNext();
        }

        private void textBox_SQLWindowsPassword_TextChanged(object sender, EventArgs e)
        {
			// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
            if (checkBox_UseSameAuth.Checked && (_comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM))
            {
                textbox_WindowsPassword.Text = textBox_SQLWindowsPassword.Text;
            }
            updateCredentialsPageMoveNext();

        }
        private void _page_Credentials_BeforeDisplay(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();

        }
        private void _page_Credentials_AfterDisplay(object sender, EventArgs e)
        {
            textBox_SQLWindowsUser.Focus();
        }

        private void _chkbx_SQLCredentials_CheckedChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_SqlLogin_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_SqlLoginPassword_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_SqlLoginConfirmPassword_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_WindowsUser_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_WindowsPassword_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _txtbx_ConfirmWindowsPassword_TextChanged(object sender, EventArgs e)
        {
            updateCredentialsPageMoveNext();
        }

        private void _page_Credentials_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            Debug.Assert(!string.IsNullOrEmpty(_txtbx_Server.Text));
            bool isWindowsCredentails = true;
            bool isOk = true;
            bool allowRegisterAnyway = true;
            string version = string.Empty;
            string edition = string.Empty;
            string login = string.Empty;
            string password = string.Empty;
            WindowsImpersonationContext targetImpersonationContext = null;
            Forms.ShowWorkingProgress showWorking = new Forms.ShowWorkingProgress();

            // Check if credentials are valid.
            string machine = string.Empty, instance = string.Empty, connection = string.Empty;

            // If sql server credentials, check to see if they have been specified.
            StringBuilder msgBldr = new StringBuilder();
            if (radioButton_SQLServerAuth.Checked)
            {
                if (string.IsNullOrEmpty(textbox_SqlLogin.Text))
                {
                    msgBldr.Append(Utility.ErrorMsgs.SqlLoginNotSpecifiedMsg);
                    isOk = false;
                    allowRegisterAnyway = false;
                }
            }
            // If windows credentials, check to see if they have been specified.
            else
            {
                if (string.IsNullOrEmpty(textBox_SQLWindowsUser.Text) || string.IsNullOrEmpty(textBox_SQLWindowsPassword.Text))
                {
                    if (msgBldr.Length > 0) { msgBldr.Append("\n\n"); }
                    msgBldr.Append(Utility.ErrorMsgs.SqlLoginNotSpecifiedMsg);
                    isOk = false;
                    allowRegisterAnyway = false;
                }

                // Check if the account format is correct.
                // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                if (isOk && ((_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerOnPremise) || (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureVM)))
                {
                    string domain = string.Empty;
                    string user = string.Empty;
                    Path.SplitSamPath(textBox_SQLWindowsUser.Text, out domain, out user);
                    if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(user))
                    {
                        if (msgBldr.Length > 0) { msgBldr.Append("\n\n"); }
                        msgBldr.Append(Utility.ErrorMsgs.SqlLoginWindowsUserNotSpecifiedMsg);
                        isOk = false;
                        allowRegisterAnyway = false;
                    }
                }
            }

            bool isPasswordLengthValid = radioButton_SQLServerAuth.Checked
            ? PasswordValidator.ValidatePasswordLength(textbox_SqlLoginPassword.Text)
            : PasswordValidator.ValidatePasswordLength(textBox_SQLWindowsPassword.Text);

            if (!isPasswordLengthValid)
            {
                isOk = false;
                allowRegisterAnyway = false;
                msgBldr.AppendFormat(Utility.Constants.PASSWORD_LENGTH_MESSAGE_FORMAT, Utility.Constants.MINIMUM_PASSWORD_LENGTH);
            }

            if (allowRegisterAnyway && _comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureDB)
            {
                // Operation System and AD User 
                if (string.IsNullOrEmpty(textbox_WindowsUser.Text) || string.IsNullOrEmpty(textbox_WindowsPassword.Text))
                {
                    if (msgBldr.Length > 0)
                    {
                        msgBldr.Append("\n\n");
                    }
                    if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerOnPremise)
                    {
                        msgBldr.Append(Utility.ErrorMsgs.WindowsUserNotSpecifiedMsg);
                    }
                    else
                    {
                        msgBldr.Append(Utility.ErrorMsgs.AzureADAccountNotSpecifiedMsg);

                    }
                    isOk = false;
                    allowRegisterAnyway = true;
                    isWindowsCredentails = false;
                }
            }

            // Check if the account format is correct.
            if (allowRegisterAnyway && isWindowsCredentails && _comboBox_ServerType.SelectedItem==Utility.Activity.TypeServerOnPremise)
            {
                string domain = string.Empty;
                string user = string.Empty;
                Path.SplitSamPath(textbox_WindowsUser.Text, out domain, out user);
                if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(user))
                {
                    if (msgBldr.Length > 0) { msgBldr.Append("\n\n"); }
                    msgBldr.Append(Utility.ErrorMsgs.WindowsUserNotSpecifiedMsg);
                    isOk = false;
                    allowRegisterAnyway = true;
                    isWindowsCredentails = false;
                }
            }


            // Get SQL Server properties and validate them.
            if (allowRegisterAnyway)
            {
                try
                {
                    showWorking.Show("Verifying SQL Server Credentials...", this);
                    if (radioButton_SQLServerAuth.Checked)
                    {
                        login = textbox_SqlLogin.Text;
                        password = textbox_SqlLoginPassword.Text;
                    } 
                    // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                    else if(radioButton_WindowsAuth.Checked &&((_comboBox_ServerType.SelectedItem==Utility.Activity.TypeServerOnPremise) || (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureVM)))
                    {
                        // Impersonate ...
                        try
                        {
                            WindowsIdentity wi =
                                Impersonation.GetCurrentIdentity(textBox_SQLWindowsUser.Text, textBox_SQLWindowsPassword.Text);
                            targetImpersonationContext = wi.Impersonate();
                        }
                        catch (Exception ex)
                        {
                            if (msgBldr.Length > 0) { msgBldr.Remove(0, msgBldr.Length); }
                            msgBldr.AppendFormat("Could not validate the windows authentication credentials for connecting to SQL Server {0}.", _txtbx_Server.Text.ToUpper());
                            msgBldr.AppendFormat("\r\n\r\nError: {0}", ex.Message);
                            logX.loggerX.Error(
                                string.Format("Error Impersonating SQL Server Windows Login User {0}: {1}",
                                textBox_SQLWindowsUser.Text, ex));
                            isOk = false;
                            allowRegisterAnyway = false;
                        }
                    }
                    string serverName = _txtbx_Server.Text;
                    if (allowRegisterAnyway)
                    {
                        // Try connecting to SQLserver...
                        try
                        {
                            showWorking.UpdateText(string.Format("Connecting to SQL Server {0}...", _txtbx_Server.Text.ToUpper()));
                            //Barkha Khatri (SQLSecure 3.1) Removing protocol name from server name
                            if (_txtbx_Server.Text.Contains(":"))
                            {
                                _txtbx_Server.Text = _txtbx_Server.Text.Split(':')[1];
                            }
                            // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            if ((_comboBox_ServerType.SelectedItem!= Utility.Activity.TypeServerOnPremise) && (_comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM))
                            {
                                serverName = _txtbx_Server.Text + "," + _txtbx_PortNumber.Text;
                            }
                            // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            bool azureADAuth = (radioButton_WindowsAuth.Checked && _comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerOnPremise && _comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureVM) ? true : false;
                            if (azureADAuth)
                            {
                                Sql.SqlServer.GetSqlServerProperties(serverName, textBox_SQLWindowsUser.Text, textBox_SQLWindowsPassword.Text,
                                                                        out version, out machine, out instance, out connection, out edition,
                                                                        (string)_comboBox_ServerType.SelectedItem, azureADAuth);
                            }
                            else
                            {
                                Sql.SqlServer.GetSqlServerProperties(serverName, login, password,
                                                                        out version, out machine, out instance, out connection, out edition,
                                                                        (string)_comboBox_ServerType.SelectedItem, azureADAuth);
                            }
                            if (targetImpersonationContext != null)
                            {
                                targetImpersonationContext.Undo();
                                targetImpersonationContext.Dispose();
                                targetImpersonationContext = null;
                            }

                            // SQLsecure 3.1 (Anshul) - Validate server edition based on server type.
                            // SQLsecure 3.1 (Anshul) - SQLSECU-1775 - Azure VM : Register a Server as Azure DB by selecting Server type as Azure VM.
                            ServerType serverType = SqlServer.GetServerTypeByName(Convert.ToString(_comboBox_ServerType.SelectedItem));
                            if (!SqlHelper.ValidateServerEdition(serverType, edition))
                            {
                                if (msgBldr.Length > 0) { msgBldr.Remove(0, msgBldr.Length); }
                                msgBldr.AppendFormat(serverType == ServerType.AzureSQLDatabase ? ErrorMsgs.IncorrectServerTypeAzureSQLDBMsg : 
                                    ErrorMsgs.IncorrectServerTypeSQLServerMsg);

                                isOk = false;
                                allowRegisterAnyway = false;
                            }

                            if (!checkVersionAndRegistration(version, connection))
                            {
                                isOk = false;
                                allowRegisterAnyway = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (msgBldr.Length > 0) { msgBldr.Remove(0, msgBldr.Length); }
                            msgBldr.AppendFormat("Could not establish a connection with SQL Server {0}.", _txtbx_Server.Text.ToUpper());
                            msgBldr.AppendFormat("\r\n\r\nError: {0}", ex.Message);
                            machine = Path.GetComputerFromSQLServerInstance(_txtbx_Server.Text);
                            instance = Path.GetInstanceFromSQLServerInstance(_txtbx_Server.Text);
                            connection = _txtbx_Server.Text;
                            isOk = false;
                            allowRegisterAnyway = false;
                            logX.loggerX.Error("Could not establish a connection with SQL Server {0}.", _txtbx_Server.Text.ToUpper());
                            logX.loggerX.Error("Exception :" + ex.Message);
                        }

                    }
                    // Undo Impersonation
                    if (targetImpersonationContext != null)
                    {
                        targetImpersonationContext.Undo();
                        targetImpersonationContext.Dispose();
                        targetImpersonationContext = null;
                    }


                    if (allowRegisterAnyway && isWindowsCredentails && _comboBox_ServerType.SelectedItem != Utility.Activity.TypeServerAzureDB)
                    {
                        // Try connecting to server
                        // Impersonate ...
                        //try
                        //{
                        //    showWorking.UpdateText("Verifying Server Credentials...");
                        //    WindowsIdentity wi =
                        //        Impersonation.GetCurrentIdentity(textbox_WindowsUser.Text, textbox_WindowsPassword.Text);
                        //    targetImpersonationContext = wi.Impersonate();
                        //}
                        //catch (Exception ex)
                        //{
                        //    if (msgBldr.Length > 0) { msgBldr.Append("\n\n"); }
                        //    msgBldr.AppendFormat("Could not validate windows authentication credentials for connecting to Server {0}.", machine);
                        //    msgBldr.AppendFormat("\r\n\r\nError: {0}", ex.Message);
                        //    logX.loggerX.Error(
                        //        string.Format("Error Impersonating Windows User for connecting to target server {0}: {1}",
                        //        textBox_SQLWindowsUser.Text, ex));
                        //    isOk = false;
                        //    allowRegisterAnyway = true;
                        //}
                        if (allowRegisterAnyway)
                        {
                            showWorking.UpdateText(string.Format("Connecting to Server {0}...", machine));
							// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            string errorMsg = string.Empty;
                            Server.ServerAccess sa = Server.ServerAccess.ERROR_OTHER;
                            // SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            if ((Convert.ToString(_comboBox_ServerType.SelectedItem)== Utility.Activity.TypeServerOnPremise) || (Convert.ToString(_comboBox_ServerType.SelectedItem) == Utility.Activity.TypeServerAzureVM))
                            {
                                sa = Server.CheckServerAccess(machine, textbox_WindowsUser.Text, textbox_WindowsPassword.Text, out errorMsg);

                                if (sa != Server.ServerAccess.OK)
                                {
                                    if ((!string.IsNullOrEmpty(serverName)) && (Convert.ToString(_comboBox_ServerType.SelectedItem) == Utility.Activity.TypeServerAzureVM))
                                    {
                                        string tempMachine = string.Empty;

                                        if (serverName.IndexOf(@"\") != -1)
                                        {
                                            tempMachine = serverName.Substring(0, serverName.IndexOf(@"\"));
                                        }
                                        else
                                        {
                                            tempMachine = serverName;
                                        }

                                        Server.ServerAccess tempSa = sa;
                                        sa = Server.ServerAccess.ERROR_OTHER;
                                        string tempErrorMsg = errorMsg;
                                        errorMsg = string.Empty;
                                        sa = Server.CheckServerAccess(tempMachine, textbox_WindowsUser.Text, textbox_WindowsPassword.Text, out errorMsg);

                                        if (sa != Server.ServerAccess.OK)
                                        {
                                            sa = tempSa;
                                            errorMsg = tempErrorMsg;
                                        }
                                        else
                                        {
                                            machine = tempMachine;
                                        }
                                    }
                                }


                                
                            }
							// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            //else if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureVM)
                            //{
                            //    sa = Server.CheckServerAccess(machine, textbox_WindowsUser.Text, textbox_WindowsPassword.Text, out errorMsg);

                            //    //if (this.radioButton_WindowsAuth.Checked)
                            //    //{
                            //    //    sa = Server.CheckAzureServerAccess(serverName, textbox_WindowsUser.Text, textbox_WindowsPassword.Text, out errorMsg, this.radioButton_WindowsAuth.Checked);
                            //    //}
                            //    //else
                            //    //{
                            //    //    sa = Server.CheckAzureServerAccess(serverName, textbox_SqlLogin.Text, textbox_SqlLoginPassword.Text, out errorMsg, this.radioButton_WindowsAuth.Checked);
                            //    //}

                            //}
							// SQLSecure 3.1 (Biresh Kumar Mishra) - Add Support for Azure VM
                            else if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureDB)
                            {
                                sa = Server.CheckAzureServerAccess(serverName, textbox_WindowsUser.Text, textbox_WindowsPassword.Text, out errorMsg, true);
                            }


                            if (sa != Server.ServerAccess.OK)
                            {
                                isOk = false;
                                if (msgBldr.Length > 0) { msgBldr.Remove(0, msgBldr.Length); }
                                msgBldr.Append("Warning:");
                                msgBldr.AppendFormat("\r\nUnable to verify if account '{0}' has admin rights to computer {1}", textbox_WindowsUser.Text, machine);
                                msgBldr.Append("\r\n\r\nDetails:\r\n");
                                msgBldr.Append(errorMsg);
                                msgBldr.Append("\r\n\r\nRecommendations:");
                                msgBldr.AppendFormat("\r\nVerify account '{0}' has admin rights to computer {1}", textbox_WindowsUser.Text, machine);
                                msgBldr.Append("\r\nVerify Firewall settings");
                                msgBldr.AppendFormat("\r\nVerify WMI settings on computer {0}", machine);
                                msgBldr.AppendFormat("\r\nVerify DCOM settings on computer {0}", machine);
                                logX.loggerX.Error(
                                    string.Format("Error Connecting to Target Server with supplied Windows User {0}\r\n{1}",
                                    textbox_WindowsUser.Text, errorMsg));
                            }
                        }
                        // Undo Impersonation
                        //if (targetImpersonationContext != null)
                        //{
                        //    targetImpersonationContext.Undo();
                        //    targetImpersonationContext.Dispose();
                        //    targetImpersonationContext = null;
                        //}
                    }
                }
                catch (Exception ex)
                {
                    if (Utility.ExceptionHelper.IsSqlLoginFailed(ex))
                    {
                        msgBldr.Append("\n\n");
                        msgBldr.Append(Utility.ErrorMsgs.SqlLoginFailureMsg);
                        msgBldr.AppendFormat("\r\n\r\nError: {0}", ex.Message);
                        isOk = false;
                    }
                    else
                    {
                        msgBldr.Append("\n\n");
                        msgBldr.Append(Utility.ErrorMsgs.RetrieveServerPropertiesFailedMsg + " " + ex.Message);
                        msgBldr.AppendFormat("\r\n\r\nError: {0}", ex.Message);
                        isOk = false;
                    }
                }
            }

            if (!isOk)
            {
                showWorking.Close();
                Activate();

                if (allowRegisterAnyway)
                {
                    msgBldr.Append("\n\n");
                    msgBldr.Append("Register Anyway?");
                    System.Windows.Forms.DialogResult dr = MsgBox.ShowWarningConfirm(ErrorMsgs.RegisterSqlServerCaption, msgBldr.ToString());
                    if (dr == DialogResult.Yes)
                    {
                        isOk = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(msgBldr.ToString()))
                    {
                        MsgBox.ShowError(ErrorMsgs.RegisterSqlServerCaption, msgBldr.ToString());
                    }
                }
            }

            // If no failures update fields.
            // Else stay on the same page.
            if (isOk)
            {
                m_Machine = machine;
                m_Instance = instance;
                
                m_Version = version;
                if (_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerOnPremise)
                {
                    m_ServerType = "OP";
                    m_Connection = connection;
                }
                else if(_comboBox_ServerType.SelectedItem == Utility.Activity.TypeServerAzureDB)
                {
                    m_ServerType = "ADB";
                    m_Connection = connection;
                    m_ConnectionPort =Int32.Parse( _txtbx_PortNumber.Text);
                }
                else
                {
                    m_ServerType = "AVM";
                    m_Connection = _txtbx_Server.Text;
                    m_ConnectionPort = Int32.Parse(_txtbx_PortNumber.Text);
                }
                addEditFoldersControl.TargetServerName = m_Machine;
            }
            else
            {
                e.Cancel = true;
            }
            showWorking.Close();
        }

        #endregion

        #region Data Collection Filter

        private void _page_DefineFilters_BeforeDisplay(object sender, EventArgs e)
        {
            m_Filter = null;
            m_Filter = new Sql.DataCollectionFilter(m_Connection, "Default rule", "Rule created when the server was registered");

            ServerVersion parsedVersion = Sql.SqlHelper.ParseVersion(m_Version);
            
            Idera.SQLsecure.UI.Console.Data.ServerInfo serverInfo = new Idera.SQLsecure.UI.Console.Data.ServerInfo(parsedVersion, radioButton_WindowsAuth.Checked,
                textBox_SQLWindowsUser.Text, textBox_SQLWindowsPassword.Text, m_Connection,(string)_comboBox_ServerType.SelectedItem);
            filterSelection1.Initialize(m_Filter, serverInfo);
        }

        private void _page_DefineFilters_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            filterSelection1.GetFilter(out m_Filter);
        }

        #endregion

        #region Schedule Page

        private void _page_JobSchedule_BeforeDisplay(object sender, EventArgs e)
        {

        }

        private void _page_JobSchedule_BeforeMoveBack(object sender, CancelEventArgs e)
        {

        }

        private void _page_JobSchedule_BeforeMoveNext(object sender, CancelEventArgs e)
        {

        }

        private void checkBox_EnableScheduling_CheckedChanged(object sender, EventArgs e)
        {
            m_scheduleData.Enabled = checkBox_EnableScheduling.Checked;

            _btn_ChangeSchedule.Enabled = m_scheduleData.Enabled;

            ScheduleJob.BuildDescription(ref m_scheduleData);
            _txtbx_ScheduleDescription.Text = m_scheduleData.Description;

        }

        private void _btn_ChangeSchedule_Click(object sender, EventArgs e)
        {
            Form_ScheduleJob form = new Form_ScheduleJob(m_scheduleData);
            if (form.ShowDialog() == DialogResult.OK)
            {
                form.GetScheduleData(out m_scheduleData);
                ScheduleJob.BuildDescription(ref m_scheduleData);
                _txtbx_ScheduleDescription.Text = m_scheduleData.Description;
            }

        }

        #endregion

        #region Options Page

        private void wizardOptionsPage_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            if (!needToConfigureSMTPProvider || (!checkBoxEmailFindings.Checked && !checkBoxEmailForCollectionStatus.Checked))
            {
                // Skip SMTP Email Config if already setup
                _page_NotificationOptions.NextPage = _page_Policies;
                if (!hasSelectablePolicy)
                {
                    // Skip Policy page if no policies are present
                    _page_NotificationOptions.NextPage = _page_CollectData;
                }
            }
            else
            {
                _page_NotificationOptions.NextPage = _page_ConfigureSMTPEmail;
            }

            if (needToConfigureEmail && (checkBoxEmailFindings.Checked || checkBoxEmailForCollectionStatus.Checked))
            {
                MsgBox.ShowWarning(ErrorMsgs.WarningEmailNoConfiguredTitle, ErrorMsgs.WarningEmailNoConfiguredMsg);
            }
        }


        #endregion

        #region Finish Page

        private string buildWizardFinishSummary(
                string server,
                string credentials,
                string filter,
                string schedule,
                string emailNotification,
                string policies,
                string collectData
            )
        {
            StringBuilder summary = new StringBuilder();
            summary.Append(WizardFinishTextPrefix);
            summary.Append(WizardFinishTextSQLServer);
            string serverRFT = server.Replace("\\", "\\\\");
            summary.Append(serverRFT);
            summary.Append(WizardFinishTextCredentials);
            summary.Append(credentials);
            summary.Append(WizardFinishTextFilters);
            summary.Append(filter);
            summary.Append(WizardFinishTextSchedule);
            summary.Append(schedule);
            summary.Append(WizardFinishTextEmailNotifications);
            summary.Append(emailNotification);
            if (!string.IsNullOrEmpty(policies))
            {
                summary.Append(WizardFinishTextPolicies);
                summary.Append(policies);
            }
            summary.Append(WizardFinishTextCollectData);
            summary.Append(collectData);
            summary.Append(WizardFinishTextSuffix);
            return summary.ToString();
        }

        private void _page_Finish_BeforeDisplay(object sender, EventArgs e)
        {
            // Build up the finish summary strings.
            string server = _txtbx_Server.Text
                            + (string.Compare(_txtbx_Server.Text, m_Connection, true) != 0 ? (" (" + m_Connection + ")") : string.Empty);
            string credentials = "Specified credentials";
            StringBuilder filter = new StringBuilder();
            filter.Append(@"\par" + m_Filter.GetFilterDetailsForSubReport() + @"\pard");
            string dataCollection = "Do not collect data after server registration.";
            if (m_nextAction == NextAction.RunCollection)
            {
                dataCollection = "Collect data after server registration.";
            }
            StringBuilder policies = new StringBuilder();
            bool firstPolicy = true;
            foreach (Policy p in m_Polices)
            {
                policies.AppendFormat("{0}{1}", firstPolicy ? "" : @"\par{{\f1\tab}} : ", p.PolicyName);
                firstPolicy = false;
            }
            StringBuilder email = new StringBuilder("No Email notification requested.");
            string emailStatus = string.Empty;
            if (checkBoxEmailForCollectionStatus.Checked)
            {
                if (radioButtonAlways.Checked)
                {
                    emailStatus = "Send Email notification for security findings at any risk.";
                }
                else if (radioButton_SendEmailWarningOrError.Checked)
                {
                    emailStatus = "Send Email notification for security findings on high and medium risks.";
                }
                else if (radioButton_SendEmailOnError.Checked)
                {
                    emailStatus = "Send Email notification for security findings only on high risks.";
                }
            }
            String emailFindings = string.Empty;
            if (checkBoxEmailFindings.Checked)
            {
                if (radioButtonSendEmailFindingAny.Checked)
                {
                    emailFindings = "Send Email notification after data collection always.";
                }
                else if (radioButtonSendEmailFindingHighMedium.Checked)
                {
                    emailFindings = "Send Email notification after data collection on Error or Warning.";
                }
                else if (radioButtonSendEmailFindingHigh.Checked)
                {
                    emailFindings = "Send Email notification after data collection only on Error.";
                }
            }
            if (checkBoxEmailForCollectionStatus.Checked || checkBoxEmailFindings.Checked)
            {
                email.Remove(0, email.Length);
                email.Append(emailStatus);
                email.Append((!string.IsNullOrEmpty(emailStatus)
                                  ? string.Format("\\par{{\\f1\\tab}} : {0}", emailFindings)
                                  : ""));
                email.AppendFormat("\\par{{\\f1\\tab}} : Recipient - {0}", textBox_Recipient.Text);
            }
            _rtb_Finish.Rtf = buildWizardFinishSummary(server.ToUpper(),
                credentials, filter.ToString(), m_scheduleData.Description, email.ToString(),
                policies.ToString(), dataCollection);
        }

        private void checkBox_CollectData_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_CollectData.Checked)
            {
                m_nextAction = NextAction.RunCollection;
            }
            else
            {
                m_nextAction = NextAction.Nothing;
            }
        }
        #endregion

        private void wizardOptionsPage_BeforeDisplay(object sender, EventArgs e)
        {
            if ((checkBoxEmailFindings.Checked || checkBoxEmailForCollectionStatus.Checked)
                && string.IsNullOrEmpty(textBox_Recipient.Text))
            {
                _page_NotificationOptions.AllowMoveNext = false;
            }
            else
            {
                _page_NotificationOptions.AllowMoveNext = true;
            }
        }

        private void SMTPDataEntered(bool entered)
        {
            _page_ConfigureSMTPEmail.AllowMoveNext = entered;
        }

        private void wizardPage1_BeforeDisplay(object sender, EventArgs e)
        {

        }

        private void _page_Introduction_BeforeDisplay(object sender, EventArgs e)
        {

        }

        private void _page_Finish_BeforeMoveBack(object sender, CancelEventArgs e)
        {
        }

        private void _page_Policies_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            m_Polices.Clear();
            foreach (UltraListViewItem policy in ultraListView_Policies.CheckedItems)
            {
                m_Polices.Add((Console.Sql.Policy)policy.Tag);
            }
        }

        private void checkBoxEmailForCollectionStatus_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonAlways.Enabled = checkBoxEmailForCollectionStatus.Checked;
            radioButton_SendEmailWarningOrError.Enabled = checkBoxEmailForCollectionStatus.Checked;
            radioButton_SendEmailOnError.Enabled = checkBoxEmailForCollectionStatus.Checked;
            if (!checkBoxEmailFindings.Checked && !checkBoxEmailForCollectionStatus.Checked)
            {
                textBox_Recipient.Enabled = false;
                _page_NotificationOptions.AllowMoveNext = true;
            }
            else
            {
                textBox_Recipient.Enabled = true;
                _page_NotificationOptions.AllowMoveNext = !string.IsNullOrEmpty(textBox_Recipient.Text);
            }
        }

        private void checkBoxEmailFindings_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonSendEmailFindingAny.Enabled = checkBoxEmailFindings.Checked;
            radioButtonSendEmailFindingHigh.Enabled = checkBoxEmailFindings.Checked;
            radioButtonSendEmailFindingHighMedium.Enabled = checkBoxEmailFindings.Checked;
            if (!checkBoxEmailFindings.Checked && !checkBoxEmailForCollectionStatus.Checked)
            {
                textBox_Recipient.Enabled = false;
                _page_NotificationOptions.AllowMoveNext = true;
            }
            else
            {
                textBox_Recipient.Enabled = true;
                _page_NotificationOptions.AllowMoveNext = !string.IsNullOrEmpty(textBox_Recipient.Text);
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void _page_CollectData_BeforeMoveBack(object sender, CancelEventArgs e)
        {
            if (!hasSelectablePolicy)
            {
                // Skip Policy page if no policies are present
                _page_CollectData.PreviousPage = _page_ConfigureSMTPEmail;

                if (!needToConfigureSMTPProvider || (!checkBoxEmailFindings.Checked && !checkBoxEmailForCollectionStatus.Checked))
                {
                    // Skip SMTP Email Config if already setup
                    _page_CollectData.PreviousPage = _page_NotificationOptions;
                }
            }
        }

        private void button_Test_Click(object sender, EventArgs e)
        {
            controlSMTPEmailConfig1.SendTestEmail();
        }

        private void _page_ConfigureSMTPEmail_BeforeDisplay(object sender, EventArgs e)
        {

        }

        private void _page_ConfigureSMTPEmail_BeforeMoveNext(object sender, CancelEventArgs e)
        {
            if (!hasSelectablePolicy)
            {
                // Skip Policy page if no policies are present
                _page_ConfigureSMTPEmail.NextPage = _page_CollectData;
            }
        }

        private void _page_Policies_BeforeMoveBack(object sender, CancelEventArgs e)
        {
            if (!needToConfigureSMTPProvider || (!checkBoxEmailFindings.Checked && !checkBoxEmailForCollectionStatus.Checked))
            {
                // Skip SMTP Email Config if already setup
                _page_Policies.PreviousPage = _page_NotificationOptions;
            }
        }

        private void textBox_Recipient_TextChanged(object sender, EventArgs e)
        {
            _page_NotificationOptions.AllowMoveNext = !string.IsNullOrEmpty(textBox_Recipient.Text);
        }

        #endregion



        private void btAdd_Click(object sender, EventArgs e)
        {
            if (Form_CreateTag.Process(null) != -1)
                RefreshGrid(true);

        }

        private void _PageTags_BeforeDisplay(object sender, EventArgs e)
        {
            ClearSelection();
            RefreshGrid(false);

        }

        private void RefreshGrid(bool clearGrid)
        {
            try
            {
              
                var tags = TagWorker.GetTags();
                if (clearGrid) ulTags.Items.Clear();
                foreach (Tag tag in tags)
                {
                    if (ulTags.Items.Exists(tag.Name))
                        continue;

                    var item = new UltraListViewItem(tag.Name, new[] { tag.Description })
                    {
                        Tag = tag.Id.ToString(),
                        Key = tag.Name
                    };
                    ulTags.Items.Add(item);

                }
            }
            catch (Exception ex)
            {
                MsgBox.ShowError(ErrorMsgs.RegisterSqlServerCaption, ex.Message, ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
           
            if (ulTags.SelectedItems.Count != 0)
            {
                int tagId;
                if (int.TryParse(ulTags.SelectedItems[0].Tag.ToString(), out tagId))
                {
                    var tag = TagWorker.GetTagById(tagId);

                    if (Form_CreateTag.Process(tag) != -1)
                        RefreshGrid(true);
                    ClearSelection();
                }

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ulTags.SelectedItems.Count != 0)
            {
                int tagId;
                if (int.TryParse(ulTags.SelectedItems[0].Tag.ToString(), out tagId))
                {
                    try
                    {
                        TagWorker.DeleteTag(tagId);
                        RefreshGrid(true);
                        
                    }
                    catch (Exception ex)
                    {
                        MsgBox.ShowError(ErrorMsgs.RegisterSqlServerCaption, ex.Message);

                    }
                    ClearSelection();
                }
            }
        }

        private void ClearSelection()
        {
            ulTags.SelectedItems.Clear();
            button2.Enabled = false;
            button3.Enabled = false;
        }

        private void ulTags_MouseDown(object sender, MouseEventArgs e)
        {
            button2.Enabled = button3.Enabled = ulTags.SelectedItems.Count != 0;        
        }

        private void _page_FilePermissionFolders_BeforeDisplay(object sender, EventArgs e)
        {

        }

        private void _page_CollectData_BeforeDisplay(object sender, EventArgs e)
        {

        }
    }
}