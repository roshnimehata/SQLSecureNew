using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Idera.SQLsecure.UI.Console.Sql;
using Infragistics.Win.UltraWinListView;
using Infragistics.Win;

namespace Idera.SQLsecure.UI.Console.Controls
{
    public partial class ControlPolicyAddServers : UserControl
    {
        private static string DynamicAllServersCode = "";
        private const string DynamicSqlVersion2000Code = "Left(version,3) ='08.'";
        private const string DynamicSqlVersion2005Code = "Left(version,3) = '09.'";
        private const string DynamicSqlVersion2008Code = "Left(version,3) = '10.'";
        //private const string DynamicSqlVersion2008R2Code = "Left(version,4) = '10.5'";
        private const string DynamicSqlVersion2012Code = "Left(version,3) = '11.'";
        private const string DynamicSqlVersion2014Code = "Left(version,3) = '12.'";
        private const string DynamicSqlVersion2016Code = "Left(version,3) = '13.'";
        private const string DynamicSqlVersionAzureCode = "servertype = 'ADB'";//SQLsecure 3.1 (Tushar)--Fix for defect SQLSECU-1728


        private const string TITLE_POLICY = "Policy";
        private const string TITLE_ASSESSMENT = "Assessment";
        private string TITLE_NAME
        {
            get { return m_policy == null ? TITLE_POLICY : (m_policy.IsAssessment ? TITLE_ASSESSMENT : TITLE_POLICY); }
        }
        private const string GROUPBOXHEADING_FMT = "Select SQL Servers to include in this {0}";

        private Policy m_policy;
        private List<RegisteredServer> m_orginalServersInPolicy = null;

        public ControlPolicyAddServers()
        {
            InitializeComponent();


        }


        public void InitializeControl(Policy policy, bool allowEdit)
        {
            m_policy = policy;

            ultraListViewServers.Items.Clear();
            foreach (RegisteredServer r in Program.gController.Repository.RegisteredServers)
            {
                UltraListViewItem li = ultraListViewServers.Items.Add(null, r.ConnectionName);
                li.Tag = r;
            }

            // load value lists for listview display
            radioButtonAll.Tag = DynamicAllServersCode;
            radioButton2000.Tag = DynamicSqlVersion2000Code;
            radioButton2005.Tag = DynamicSqlVersion2005Code;
            radioButton2008.Tag = DynamicSqlVersion2008Code;
            //radioButton2008R2.Tag = DynamicSqlVersion2008R2Code;
            radioButton2012.Tag = DynamicSqlVersion2012Code;
            radioButton2014.Tag = DynamicSqlVersion2014Code;
            radioButton2016.Tag = DynamicSqlVersion2016Code;
            radioButtonAzure.Tag = DynamicSqlVersionAzureCode;

            groupBoxMainServerSelection.Text = string.Format(GROUPBOXHEADING_FMT, TITLE_NAME);

            radioButtonAll.Checked = true;
            if (m_policy.IsDynamic)
            {
                switch (m_policy.DynamicSelection)
                {
                    case DynamicSqlVersion2000Code:
                        radioButton2000.Checked = true;
                        break;
                    case DynamicSqlVersion2005Code:
                        radioButton2005.Checked = true;
                        break;
                    case DynamicSqlVersion2008Code:
                        radioButton2008.Checked = true;
                        break;
                    //case DynamicSqlVersion2008R2Code:
                    //    radioButton2008R2.Checked = true;
                    //    break;
                    case DynamicSqlVersion2012Code:
                        radioButton2012.Checked = true;
                        break;
                    case DynamicSqlVersion2014Code:
                        radioButton2014.Checked = true;
                        break;
                    case DynamicSqlVersion2016Code:
                        radioButton2016.Checked = true;
                        break;
                    case DynamicSqlVersionAzureCode:
                        radioButtonAzure.Checked = true;
                        break;
                    default:
                        radioButtonAll.Checked = true;
                        break;
                }
            }
            else
            {
                radioButtonManual.Checked = true;
            }

            m_orginalServersInPolicy = m_policy.GetMemberServers();
            foreach (UltraListViewItem server in ultraListViewServers.Items)
            {
                if (m_orginalServersInPolicy.Contains((RegisteredServer)server.Tag))
                {
                    server.CheckState = CheckState.Checked;
                }
            }

            if (!allowEdit)
            {
                radioButtonAll.Enabled = false;
                radioButton2000.Enabled = false;
                radioButton2005.Enabled = false;
                radioButton2008.Enabled = false;
                //radioButton2008R2.Enabled = false;
                radioButton2012.Enabled = false;
                radioButton2014.Enabled = false;
                radioButton2016.Enabled = false;
                radioButtonAzure.Enabled = false;
                radioButtonManual.Enabled = false;
                ultraListViewServers.Enabled = false;
            }
            else
            {
                if (m_policy != null && m_policy.IsDynamic)
                {
                    foreach (UltraListViewItem lvi in ultraListViewServers.Items)
                    {
                        lvi.Enabled = false;
                    }
                }

                bool isEnabled = (m_policy == null ||
                    (m_policy.IsSystemPolicy &&
                                m_policy.IsDynamic)) ? false : m_policy.IsPolicy;
                        radioButtonAzure.Enabled = 
                        radioButtonAll.Enabled =
                        radioButton2000.Enabled =
                        radioButton2005.Enabled =
                        radioButton2008.Enabled =
                        //radioButton2008R2.Enabled =
                        radioButton2012.Enabled =
                        radioButton2014.Enabled =
                        radioButton2016.Enabled = isEnabled;
            }
        }

        public List<string> GetServerText()
        {
            List<string> servers = new List<string>();
            if (radioButtonManual.Checked)
            {
                foreach (UltraListViewItem server in ultraListViewServers.CheckedItems)
                {
                    servers.Add(server.Text);
                }
            }
            else if (radioButtonAll.Checked)
            {
                servers.Add(radioButtonAll.Text);
            }
            else if (radioButton2000.Checked)
            {
                servers.Add(radioButton2000.Text);
            }
            else if (radioButton2005.Checked)
            {
                servers.Add(radioButton2005.Text);
            }
            else if (radioButton2008.Checked)
            {
                servers.Add(radioButton2008.Text);
            }
            //else if (radioButton2008R2.Checked)
            //{
            //    servers.Add(radioButton2008R2.Text);
            //}
            else if (radioButton2012.Checked)
            {
                servers.Add(radioButton2012.Text);
            }
            else if (radioButton2014.Checked)
            {
                servers.Add(radioButton2014.Text);
            }
            else if (radioButton2016.Checked)
            {
                servers.Add(radioButton2016.Text);
            }
            else if (radioButtonAzure.Checked)
            {
                servers.Add(radioButtonAzure.Text);
            }
            return servers;
        }


        public void GetServers(bool createNew,
                                out List<RegisteredServer> serversToAdd,
                                out List<RegisteredServer> serversToRemove,
                                out string dynamicSelection,
                                out bool isDynamic
                               )
        {

            serversToAdd = new List<RegisteredServer>();
            serversToRemove = new List<RegisteredServer>();
            dynamicSelection = string.Empty;
            isDynamic = !radioButtonManual.Checked;

            if (!isDynamic)
            {
                foreach (UltraListViewItem server in ultraListViewServers.CheckedItems)
                {
                    serversToAdd.Add((RegisteredServer) server.Tag);
                }
            }

            if (!createNew && m_orginalServersInPolicy != null)
            {
                foreach (RegisteredServer r in m_orginalServersInPolicy)
                {
                    bool isStillChecked = false;
                    foreach (UltraListViewItem server in ultraListViewServers.CheckedItems)
                    {
                        if ((RegisteredServer)server.Tag == r)
                        {
                            isStillChecked = true;
                            break;
                        }
                    }
                    if (!isStillChecked)
                    {
                        serversToRemove.Add(r);
                    }
                }

            }

            if (radioButtonAll.Checked)
            {
                dynamicSelection = (string)radioButtonAll.Tag;
            }
            else if (radioButton2000.Checked)
            {
                dynamicSelection = (string)radioButton2000.Tag;
            }
            else if (radioButton2005.Checked)
            {
                dynamicSelection = (string)radioButton2005.Tag;
            }
            else if (radioButton2008.Checked)
            {
                dynamicSelection = (string)radioButton2008.Tag;
            }
            //else if (radioButton2008R2.Checked)
            //{
            //    dynamicSelection = (string)radioButton2008R2.Tag;
            //}
            else if (radioButton2012.Checked)
            {
                dynamicSelection = (string)radioButton2012.Tag;
            }
            else if (radioButton2014.Checked)
            {
                dynamicSelection = (string)radioButton2014.Tag;
            }
            else if (radioButton2016.Checked)
            {
                dynamicSelection = (string)radioButton2016.Tag;
            }
            else if (radioButtonAzure.Checked)
            {
                dynamicSelection = (string)radioButtonAzure.Tag;
            }
        }

        public int NumServers
        {
            get
            {
                if (!radioButtonManual.Checked)
                {
                    return -1;
                }
                else
                {
                    return ultraListViewServers.CheckedItems.Count;
                }
            }
        }

        private void radioButtonManual_CheckedChanged(object sender, EventArgs e)
        {
            foreach (UltraListViewItem li in ultraListViewServers.Items)
            {
                li.Enabled = radioButtonManual.Checked;
                li.CheckState = CheckState.Unchecked;
            }
        }

    }            
           
}
