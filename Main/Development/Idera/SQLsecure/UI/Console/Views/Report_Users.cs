using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Idera.SQLsecure.Core.Accounts;
using Idera.SQLsecure.Core.Logger;
using Idera.SQLsecure.UI.Console.Utility;

namespace Idera.SQLsecure.UI.Console.Views
{
    public partial class Report_Users : Idera.SQLsecure.UI.Console.Controls.BaseReport, Interfaces.IView
    {
        private static LogX logX = new LogX("Idera.SQLsecure.UI.Console.Views.Report_Users"); 

        #region IView

        void Interfaces.IView.SetContext(Interfaces.IDataContext contextIn)
        {
            Data.Report context = (Data.Report)contextIn;
            
            if (context.RunReport && _button_RunReport.Enabled)
            {
                runReport();
            }
        }

        #endregion

        #region Ctors

        public Report_Users()
        {
            InitializeComponent();

            //set the global title, the report title in the header graphic, and the title in the
            //status bar
            m_Title                     =
                _label_ReportTitle.Text =
                _label_Status.Text      = Utility.Constants.ReportTitle_Users;

            //set the description and getting started text
            _label_Description.Text = Utility.Constants.ReportSummary_Users;
            _label_Instructions.Text = Utility.Constants.ReportRunInstructions_NoParameters;

            _button_RunReport.Enabled = true;
        }
        
        #endregion

        #region Queries & Constants

        // Main Report
        private const string QueryDataSource = @"SQLsecure.dbo.isp_sqlsecure_report_getsqlsecurelogins";
        private const string DataSourceName = @"ReportsDataset_isp_sqlsecure_report_getsqlsecurelogins";

        #endregion        

        #region Helpers

        protected internal override void runReport()
        {
            base.runReport();

            logX.loggerX.Info(@"Retrieve data for report SQLsecure Users");

            try
            {
                using (SqlConnection connection = new SqlConnection(Program.gController.Repository.ConnectionString))
                {
                    // Open the connection.
                    connection.Open();

                    // Setup stored procedure
                    SqlCommand cmd = new SqlCommand(QueryDataSource, connection);
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Get data
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    ReportDataSource rds = new ReportDataSource();
                    rds.Name = DataSourceName;
                    rds.Value = ds.Tables[0];
                    _reportViewer.LocalReport.DataSources.Clear();
                    _reportViewer.LocalReport.DataSources.Add(rds);
                }
            }
            catch (SqlException ex)
            {
                logX.loggerX.Error(@"Error - Unable to get report data", ex);
                MsgBox.ShowError(m_Title, ErrorMsgs.CantGetReportData, ex);
            }

            //add report parameters
            ReportParameter[] Param = new ReportParameter[1];
            Param[0] = new ReportParameter("ReportTitle", string.Format(Utility.Constants.REPORTS_TITLE_STR, m_Title));

            _reportViewer.LocalReport.EnableHyperlinks = true;
            _reportViewer.LocalReport.SetParameters(Param);

            // Make sure _reportViewer is created.
            // We had issues on some servers where the _reportViewer was not yet created
            // and this caused a crash.
            if (!_reportViewer.IsHandleCreated)
            {
                if (!_reportViewer.Created)
                {
                    _reportViewer.CreateControl();
                }
            }

            _reportViewer.RefreshReport();
        }

        #endregion
    }
}