using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Idera.SQLsecure.Core.Logger;
using Idera.SQLsecure.UI.Console.Utility;

namespace Idera.SQLsecure.UI.Console.Forms
{
    public partial class Form_ConnectRepository : Idera.SQLsecure.UI.Console.Controls.BaseDialogForm
    {
        #region Ctors
        //SQLsecure 3.1 (Tushar)--Added integer variable to identify the type of server and authentiction selected.
        int selected_authentication_type;
        int typeOfServer;

        bool isConnect = true;
        string button_value = "Connect";
        int button_index = 0;
        bool areCredentialsRequired = false;
        public Form_ConnectRepository(bool isConnect = true)
        {
            this.isConnect = isConnect;
            //Show button text as per Connect/Deploy Repository
            if(isConnect == false)
            {
                button_value = "Deploy";
                button_index = 1;
            }
            InitializeComponent();
        }

        #endregion

        #region Fields

        private static LogX logX = new LogX("Idera.SQLsecure.UI.Console.Forms.Form_ConnectRepository");
        private string m_User = "";
        private string m_Password = "";

        #endregion

        #region Properties

        public string Server
        {
            get { return _textBox_Server.Text; }
            set { _textBox_Server.Text = value; }
        }

        public string User
        {
            get { return m_User; }
        }

        public string Password
        {
            get { return m_Password; }
        }

        #endregion

        #region Events

        private void _button_Lookup_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            Form_SelectServer dlg = new Form_SelectServer();

            try
            {
                if (dlg.LoadServers())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _textBox_Server.Text = dlg.SelectedServer;
                    }
                }
            }
            catch (Exception ex)
            {
                logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantValidateRepository), ex);
                MsgBox.ShowError(Utility.ErrorMsgs.CantValidateRepository, ex);
            }
            this.Cursor = Cursors.Default;
        }

        private void _textBox_Server_TextChanged(object sender, EventArgs e)
        {
            if (_textBox_Server.Text.Trim().Length == 0)
            {
                _button_OK.Enabled = false;
            }
            else
            {
                _button_OK.Enabled = true;
            }
        }

        private void _button_OK_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            if (!string.IsNullOrEmpty(this._textBox_Server.Text))
            {
                MainForm.Server_Name = this._textBox_Server.Text;
                //SQLsecure 3.1 (Tushar)--Saving values for type of authentication and server.
                MainForm.typeOfAuthentication = (int)selected_authentication_type;
                MainForm.typeOfServer = typeOfServer;
                if (areCredentialsRequired)
                {
                    if ((!string.IsNullOrEmpty(this.username.Text)) && (!string.IsNullOrEmpty(this.password.Text)))
                    {
                        MainForm.UserName = this.username.Text;
                        MainForm.Password = this.password.Text;
                    }
                    else
                    {
                        DialogResult = DialogResult.None;
                        MessageBox.Show("Please enter username and password to access selected SQL server instance.", "Repository", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                //Start-SQLsecure 3.1 (Tushar)--Supporting windows auth for repository connection
                if (MainForm.typeOfAuthentication == 0)
                {
                    if (this.username.Text.Length == 0 && this.password.Text.Length == 0)
                    {
                        if (MessageBox.Show("Windows login credentials are not specified. You will not be able to connect to the repository on other computer. Do you want to connect to repository on this computer?", "Repository", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            DialogResult = DialogResult.None;
                        }
                    }
                    else
                    {
                        MainForm.UserName = this.username.Text;
                        MainForm.Password = this.password.Text;
                    }
                }
                //End-SQLsecure 3.1 (Tushar)--Supporting windows auth for repository connection

            }
            else
            {
                DialogResult = DialogResult.None;
                MessageBox.Show("Please enter SQL server instance name.", "Repository", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        #endregion

        private void ultraButton_Help_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void Form_ConnectRepository_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            ShowHelp();
        }

        private void ShowHelp()
        {
            Program.gController.ShowTopic(Utility.Help.ConnectRepositoryHelpTopic);
        }

        //SQLSecure3.1 (Mitul Kapoor) - update button based on user selection of radio buttons
        private void Action_choice_ValueChanged(object sender, System.EventArgs e)
        {
            var selection = sender as Infragistics.Win.UltraWinEditors.UltraOptionSet;
            if (selection.CheckedItem.DataValue.Equals("Connect"))
            {
                this._button_OK.Text = "Connect";
                MainForm.isConnect = true;
                this.Text = "Connect to Repository";
                this.Description = "Connect to SQLsecure Repository";
            }
            else
            {
                this._button_OK.Text = "Deploy";
                MainForm.isConnect = false;
                this.Text = "Deploy Repository";
                this.Description = "Designate a database instance that will host the SQLsecure Repository. Select SQL Server and then select the appropriate options to install the Repository.";
            }
          }

        private void label3_Click(object sender, EventArgs e)
        {

        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //
            // This changes the main window text when you type into the TextBox.
            //
            if(!String.IsNullOrEmpty(this.username.Text))
                this._button_OK.Enabled = true;
        }

        //SQLSecure3.1 (Mitul Kapoor) - functionality for sql authentication
        private void sql_authentication_CheckedChanged(object sender, EventArgs e)
        {
            //SQLsecure 3.1 (Tushar)--Saving values for type of authentication and server.
            typeOfServer = (int)Constants.typeOfServer.remoteVM;
            selected_authentication_type = (int)Utility.type_of_authentication.sa;
            this.username.Enabled = true;
            this.password.Enabled = true;
            areCredentialsRequired = true;
            if (this.username.Text == null || this.username.Text == "")
            {
                this._button_OK.Enabled = false;
            }
            else
            {
                this._button_OK.Enabled = true;
            }

        }

        //SQLSecure3.1 (Mitul Kapoor) - functionality for windows authentication
        private void windows_authentication_CheckedChanged(object sender, EventArgs e)
        {
            //SQLsecure 3.1 (Tushar)--Saving values for type of authentication and server.
            typeOfServer = (int)Constants.typeOfServer.onPremise;
            selected_authentication_type = (int)Utility.type_of_authentication.windows;
            //Start-SQLsecure 3.1 (Tushar)--Supporting windows auth for repository connection
            this.username.Enabled = true;
            this.password.Enabled = true;
            areCredentialsRequired = false;
            this._button_OK.Enabled = true;
            //End-SQLsecure 3.1 (Tushar)--Supporting windows auth for repository connection
        }

        //SQLSecure3.1 (Mitul Kapoor) - functionality for azure AD authentication
        private void azure_authentication_CheckedChanged(object sender, EventArgs e)
        {
            //SQLsecure 3.1 (Tushar)--Saving values for type of authentication and server.
            typeOfServer = (int)Constants.typeOfServer.azureVM;
            selected_authentication_type = (int)Utility.type_of_authentication.sa;

            this.username.Enabled = true;
            this.password.Enabled = true;
            areCredentialsRequired = true;
            if (this.username.Text == null || this.username.Text == "")
            {
                this._button_OK.Enabled = false;
            }
            else
            {
                this._button_OK.Enabled = true;
            }

        }

        private void Form_Load(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

    }
}
