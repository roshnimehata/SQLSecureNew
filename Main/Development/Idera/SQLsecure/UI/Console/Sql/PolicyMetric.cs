/******************************************************************
 * Name: PolicyMetric.cs
 *
 * Description: Encapsulates a SQLsecure security policy PolicyMetric (now called security check).
 *
 *
 * Assemblies/DLLs needed:
 *
 * (C) 2007 - Idera, a division of BBS Technologies, Inc.
 *******************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Xml.Serialization;

using Idera.SQLsecure.Core.Logger;
using Idera.SQLsecure.UI.Console.Utility;

namespace Idera.SQLsecure.UI.Console.Sql
{
    /// <summary>
    /// Encapsulates a SQLsecure security PolicyMetric
    /// </summary>
    public class PolicyMetric
    {
        #region Fields and Enums

        private SqlInt32 m_PolicyId;
        private SqlInt32 m_AssessmentId;
        private SqlString m_PolicyName;
        private SqlInt32 m_MetricId;
        private SqlString m_MetricType;
        private SqlString m_MetricName;
        private SqlString m_MetricDescription;
        private SqlBoolean m_IsUserEntered;
        private SqlBoolean m_IsMultiSelect;
        private SqlString m_ValidValues;
        private SqlString m_ValueDescription;
        private SqlBoolean m_IsEnabled;
        private SqlString m_ReportKey;
        private SqlString m_ReportText;
        private SqlInt32 m_Severity;
        private SqlString m_SeverityValues;

        [XmlIgnoreAttribute]
        private static LogX logX = new LogX("Idera.SQLsecure.UI.Console.Sql.PolicyMetric");
        [XmlIgnoreAttribute]
        private bool m_dirty = false;


        private enum PolicyColumn
        {
            PolicyId = 0,
            AssessmentId,
            PolicyName,
            MetricId,
            MetricType,
            MetricName,
            MetricDescription,
            IsUserEntered,
            IsMultiSelect,
            ValidValues,
            ValueDescription,
            IsEnabled,
            ReportKey,
            ReportText,
            Severity,
            SeverityValues
        }


        #endregion

        #region Querries

        private const string QueryGetMetrics =
            @"SELECT 
                        policyid,
                        assessmentid,
                        policyname,
                        metricid,
                        metrictype,
                        metricname,
                        metricdescription,
                        isuserentered,
                        ismultiselect,
                        validvalues,
                        valuedescription,
                        isenabled,
                        reportkey,
                        reporttext,
                        severity,
                        severityvalues
                FROM SQLsecure.dbo.vwpolicymetric 
                WHERE policyid = @policyid";

        private const string QueryGetPolicyMetrics = QueryGetMetrics +
                                                     @" AND assessmentstate = N'" + Utility.Policy.AssessmentState.Settings + @"'";
        private const string QueryGetAssessmentMetrics = QueryGetMetrics +
                                                     @" AND assessmentid = @assessmentid";

        private const string ParamPolicyId = "policyid";
        private const string ParamAssessmentId = "@assessmentid";
        private const string NonQueryUpdatePolicyMetrics = @"SQLsecure.dbo.isp_sqlsecure_updatepolicymetric";

        private const string SPParamPolicyId = "@policyid";
        private const string SPParamAssessmentId = "@assessmentid";
        private const string SPParamMetricId = "@metricid";
        private const string SPParamIsEnabled = "@isenabled";
        private const string SPParamReportKey = "@reportkey";
        private const string SPParamReportText = "@reporttext";
        private const string SPParamSeverity = "@severity";
        private const string SPParamSeverityValues = "@severityvalues";

        #endregion

        #region Ctors

        public PolicyMetric(SqlDataReader rdr)
        {
            setValues(rdr);
        }

        public PolicyMetric()
        { }

        #endregion

        #region Properties

        [XmlIgnoreAttribute]
        public int PolicyId
        {
            get { return m_PolicyId.IsNull ? 0 : m_PolicyId.Value; }
            set
            {
                if (m_PolicyId.IsNull || value != m_PolicyId.Value)
                {
                    //m_dirty = true;
                    m_PolicyId = value;
                }
            }
        }
        [XmlIgnoreAttribute]
        public int AssessmentId
        {
            get { return m_AssessmentId.IsNull ? 0 : m_AssessmentId.Value; }
            set
            {
                if (m_AssessmentId.IsNull || value != m_AssessmentId.Value)
                {
                    m_dirty = true;
                    m_AssessmentId = value;
                }
            }
        }
        public string PolicyName
        {
            get { return m_PolicyName.IsNull ? string.Empty : m_PolicyName.Value; }
            //set
            //{
            //    if (m_PolicyName.IsNull || value != m_PolicyName.Value)
            //    {
            //        m_dirty = true;
            //        m_PolicyName = value;
            //    }
            //}
        }
        public int MetricId
        {
            get { return m_MetricId.IsNull ? 0 : m_MetricId.Value; }
            set
            {
                if (m_MetricId.IsNull || value != m_MetricId.Value)
                {
                    m_dirty = true;
                    m_MetricId = value;
                }
            }
        }
        public string MetricType
        {
            get { return m_MetricType.IsNull ? string.Empty : m_MetricType.Value; }
            //set
            //{
            //    if (m_MetricType.IsNull || value != m_MetricType.Value)
            //    {
            //        m_dirty = true;
            //        m_MetricType = value;
            //    }
            //}
        }
        public string MetricName
        {
            get { return m_MetricName.IsNull ? string.Empty : m_MetricName.Value; }
            //set
            //{
            //    if (m_MetricName.IsNull || value != m_MetricName.Value)
            //    {
            //        m_dirty = true;
            //        m_MetricName = value;
            //    }
            //}
        }
        public string MetricDescription
        {
            get { return m_MetricDescription.IsNull ? string.Empty : m_MetricDescription.Value; }
            //set
            //{
            //    if (m_MetricDescription.IsNull || value != m_MetricDescription.Value)
            //    {
            //        m_dirty = true;
            //        m_MetricDescription = value;
            //    }
            //}
        }
        public bool IsUserEntered
        {
            get { return m_IsUserEntered.IsNull ? false : m_IsUserEntered.Value; }
            //set
            //{
            //    if (m_IsUserEntered.IsNull || value != m_IsUserEntered.Value)
            //    {
            //        m_dirty = true;
            //        m_IsUserEntered = value;
            //    }
            //}
        }
        public bool IsMultiSelect
        {
            get { return m_IsMultiSelect.IsNull ? false : m_IsMultiSelect.Value; }
            //set
            //{
            //    if (m_IsMultiSelect.IsNull || value != m_IsMultiSelect.Value)
            //    {
            //        m_dirty = true;
            //        m_IsMultiSelect = value;
            //    }
            //}
        }
        public string ValidValues
        {
            get { return m_ValidValues.IsNull ? string.Empty : m_ValidValues.Value; }
            //set
            //{
            //    if (m_ValidValues.IsNull || value != m_ValidValues.Value)
            //    {
            //        m_dirty = true;
            //        m_ValidValues = value;
            //    }
            //}
        }
        public string ValueDescription
        {
            get { return m_ValueDescription.IsNull ? string.Empty : m_ValueDescription.Value; }
            //set
            //{
            //    if (m_ValueDescription.IsNull || value != m_ValueDescription.Value)
            //    {
            //        m_dirty = true;
            //        m_ValueDescription = value;
            //    }
            //}
        }
        public bool IsEnabled
        {
            get { return m_IsEnabled.IsNull ? false : m_IsEnabled.Value; }
            set
            {
                if (m_IsEnabled.IsNull || value != m_IsEnabled.Value)
                {
                    m_dirty = true;
                    m_IsEnabled = value;
                }
            }
        }
        public string ReportKey
        {
            get { return m_ReportKey.IsNull ? string.Empty : m_ReportKey.Value; }
            set
            {
                if (m_ReportKey.IsNull || value != m_ReportKey.Value)
                {
                    m_dirty = true;
                    m_ReportKey = value;
                }
            }
        }
        public string ReportText
        {
            get { return m_ReportText.IsNull ? string.Empty : m_ReportText.Value; }
            set
            {
                if (m_ReportText.IsNull || value != m_ReportText.Value)
                {
                    m_dirty = true;
                    m_ReportText = value;
                }
            }

        }
        public int Severity
        {
            get { return m_Severity.IsNull ? 0 : m_Severity.Value; }
            set
            {
                if (m_Severity.IsNull || value != m_Severity.Value)
                {
                    m_dirty = true;
                    m_Severity = value;
                }
            }
        }
        public string SeverityValues
        {
            get { return m_SeverityValues.IsNull ? string.Empty : m_SeverityValues.Value; }
            set
            {
                if (m_SeverityValues.IsNull || value != m_SeverityValues.Value)
                {
                    m_dirty = true;
                    m_SeverityValues = value;
                }
            }
        }

        #endregion

        #region Methods

        public void SetMetricChanged()
        {
            m_dirty = true;
        }

        public bool UpdatePolicyMetricsToRepository(string connectionString)
        {
            bool saved = false;

            if (m_dirty)
            {
                try
                {
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        // Retrieve server information.
                        logX.loggerX.Info("Update Policies Metrics");

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            // Open the connection.
                            connection.Open();
                            SqlParameter paramPolicyId = new SqlParameter(SPParamPolicyId, PolicyId);
                            SqlParameter paramAssessmentId = new SqlParameter(SPParamAssessmentId, AssessmentId);
                            SqlParameter paramMetricId = new SqlParameter(SPParamMetricId, MetricId);
                            SqlParameter paramIsEnabled = new SqlParameter(SPParamIsEnabled, IsEnabled);
                            SqlParameter paramReportKey = new SqlParameter(SPParamReportKey, ReportKey);
                            SqlParameter paramReportText = new SqlParameter(SPParamReportText, ReportText);
                            SqlParameter paramSeverity = new SqlParameter(SPParamSeverity, Severity);
                            SqlParameter paramSeverityValues = new SqlParameter(SPParamSeverityValues, SeverityValues);

                            Sql.SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure,
                                                          NonQueryUpdatePolicyMetrics, new SqlParameter[]
                                                                                           {
                                                                                               paramPolicyId,
                                                                                               paramAssessmentId,
                                                                                               paramMetricId,
                                                                                               paramIsEnabled,
                                                                                               paramReportKey,
                                                                                               paramReportText,
                                                                                               paramSeverity,
                                                                                               paramSeverityValues
                                                                                           });
                        }

                        saved = true;
                        m_dirty = false;
                    }
                }
                catch (SqlException ex)
                {
                    logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantUpdatePolicy), ex);
                    MsgBox.ShowError(Utility.ErrorMsgs.CantUpdatePolicy, ex.Message);
                }
                catch (Exception ex)
                {
                    logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantUpdatePolicy), ex);
                    MsgBox.ShowError(Utility.ErrorMsgs.CantUpdatePolicy, ex.Message);
                }
            }

            return saved;
        }

        public static List<PolicyMetric> GetPolicyMetrics(string connectionString, int policyId, int? assessmentId)
        {
            List<PolicyMetric> policyMetricList = new List<PolicyMetric>();
            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Retrieve server information.
                    logX.loggerX.Info("Retrieve Policies Metrics");

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // Open the connection.
                        connection.Open();
                        SqlParameter paramPolicyId = new SqlParameter(ParamPolicyId, policyId);
                        SqlParameter paramAssessmentId = new SqlParameter(ParamAssessmentId, assessmentId);
                        using (SqlDataReader rdr = Sql.SqlHelper.ExecuteReader(connection, null, CommandType.Text,
                                                                                assessmentId.HasValue ? QueryGetAssessmentMetrics : QueryGetPolicyMetrics,
                                                                                assessmentId.HasValue ? new SqlParameter[] { paramPolicyId, paramAssessmentId } : new SqlParameter[] { paramPolicyId }))
                        {
                            while (rdr.Read())
                            {
                                PolicyMetric policyMetric = new PolicyMetric(rdr);

                                policyMetricList.Add(policyMetric);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantGetPolicies), ex);
                MsgBox.ShowError(Utility.ErrorMsgs.CantGetPolicies, ex.Message);
            }
            catch (Exception ex)
            {
                logX.loggerX.Error(string.Format(Utility.ErrorMsgs.ErrorStub, Utility.ErrorMsgs.CantGetPolicies), ex);
                MsgBox.ShowError(Utility.ErrorMsgs.CantGetPolicies, ex.Message);
            }

            return policyMetricList;
        }

        #endregion

        #region Helpers

        private void setValues(SqlDataReader rdr)
        {
            m_PolicyId = rdr.GetSqlInt32((int)PolicyColumn.PolicyId);
            m_AssessmentId = rdr.GetSqlInt32((int)PolicyColumn.AssessmentId);
            m_PolicyName = rdr.GetSqlString((int)PolicyColumn.PolicyName);
            m_MetricId = rdr.GetSqlInt32((int)PolicyColumn.MetricId);
            m_MetricType = rdr.GetSqlString((int)PolicyColumn.MetricType);
            m_MetricName = rdr.GetSqlString((int)PolicyColumn.MetricName);
            m_MetricDescription = rdr.GetSqlString((int)PolicyColumn.MetricDescription);
            m_IsUserEntered = rdr.GetSqlBoolean((int)PolicyColumn.IsUserEntered);
            m_IsMultiSelect = rdr.GetSqlBoolean((int)PolicyColumn.IsMultiSelect);
            m_ValidValues = rdr.GetSqlString((int)PolicyColumn.ValidValues);
            m_ValueDescription = rdr.GetSqlString((int)PolicyColumn.ValueDescription);
            m_IsEnabled = rdr.GetSqlBoolean((int)PolicyColumn.IsEnabled);
            m_ReportKey = rdr.GetSqlString((int)PolicyColumn.ReportKey);
            m_ReportText = rdr.GetSqlString((int)PolicyColumn.ReportText);
            m_Severity = rdr.GetSqlInt32((int)PolicyColumn.Severity);
            m_SeverityValues = rdr.GetSqlString((int)PolicyColumn.SeverityValues);
        }

        #endregion
    }

    public class MetricComparer : IComparer<PolicyMetric>
    {
        public int Compare(PolicyMetric x, PolicyMetric y)
        {
            if (x.MetricId == y.MetricId)
            {
                return 0;
            }
            if (x.MetricId < y.MetricId)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}