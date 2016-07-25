﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using Idera.SQLsecure.Core.Accounts;
using Idera.SQLsecure.Core.Logger;
using Idera.SQLsecure.UI.Console.Forms;
using Idera.SQLsecure.UI.Console.Import.Models;
using Idera.SQLsecure.UI.Console.Sql;

namespace Idera.SQLsecure.UI.Console.Import
{
    public class ServerImportManager
    {


        private static LogX logX = new LogX("Idera.SQLsecure.UI.Console.Import.ServerImportManager");
        public static bool ImportItem(ImportItem itemToImport, Repository repository)
        {
            return ImportItem(itemToImport, repository, ImportSettings.Default);
        }

        public static bool ImportItem(ImportItem itemToImport, Repository repository, ImportSettings settings)
        {
            using (logX.loggerX.InfoCall())
            {


                string sqlServerVersion, machine, instance, connectionName, errorMsg;
                var errorList = new List<string>();

                if (itemToImport.HasErrors())
                {
                    settings.ChangeStatus(ImportStatusIcon.Warning,
                        string.Format("Skipped due to errors: {0}", itemToImport.GetErrors()));
                    return false;
                }
                settings.ChangeStatus(ImportStatusIcon.Importing, "Importing");


                var serverNameParts = itemToImport.ServerName.Split(',');

                var serverName = serverNameParts[0];
                var serverPort = serverNameParts.Length > 1 ? Convert.ToInt32(serverNameParts[1]) : (int?)null;
                ScheduleJob.ScheduleData scheduleData = new ScheduleJob.ScheduleData();

                var isSuccessfulOperation =
                    ValidateCredentials(itemToImport, out sqlServerVersion, out machine, out instance,
                        out connectionName, out errorMsg) &&
                    CheckServerAccess(itemToImport, machine, out errorMsg);

                if (!isSuccessfulOperation)
                {
                    settings.ChangeStatus(ImportStatusIcon.Error, string.Format("Not imported. {0}", errorMsg));
                    return false; //Skip importing of server
                }

                try
                {
                    if (repository.RegisteredServers.Find(connectionName) != null)
                    {
                        UpdateCredentials(itemToImport, repository, connectionName);
                        settings.ChangeStatus(ImportStatusIcon.Imported, "Updated");
                        return true;
                    }
                    RegisteredServer.AddServer(repository.ConnectionString,
                        connectionName, serverPort, machine, instance,
                        itemToImport.AuthType == SqlServerAuthenticationType.WindowsAuthentication ? "W" : "S",
                        itemToImport.UserName, itemToImport.Password,
                        itemToImport.UseSameCredentials ? itemToImport.UserName : itemToImport.WindowsUserName,
                        itemToImport.UseSameCredentials
                            ? itemToImport.Password
                            : itemToImport.WindowsUserPassword,
                        sqlServerVersion, 0, new string[0]);
                    repository.RefreshRegisteredServers();
                }
                catch (Exception ex)
                {
                    errorList.Add(ex.Message);

                    logX.loggerX.Error(ex.Message);
                    return false;
                }


                // Add rules to the repository.
                try
                {
                    AddRulesToRepository(repository, instance, connectionName);
                }
                catch (Exception ex)
                {

                    errorList.Add(ex.Message);
                }

                // Add job to repository
                try
                {
                    AddJobToRepository(repository, scheduleData, connectionName, serverName);
                }
                catch (Exception ex)
                {
                    errorList.Add(ex.Message);
                }

                settings.ChangeStatus(ImportStatusIcon.Imported,
                    errorList.Count > 0
                        ? string.Format("Imported with errors: {0}", string.Join("\n", errorList.ToArray()))
                        : "Imported");
                return isSuccessfulOperation;
            }
        }

        private static void UpdateCredentials(ImportItem itemToImport, Repository repository, string connectionName)
        {
            RegisteredServer.UpdateCredentials(repository.ConnectionString,
                connectionName, itemToImport.UserName, itemToImport.Password,
                itemToImport.AuthType == SqlServerAuthenticationType.WindowsAuthentication ? "W" : "S",
                itemToImport.UseSameCredentials ? itemToImport.UserName : itemToImport.WindowsUserName,
                itemToImport.UseSameCredentials
                    ? itemToImport.Password
                    : itemToImport.WindowsUserPassword);
        }

        private static void AddRulesToRepository(Repository repository, string instance, string connectionName)
        {
            try
            {
                var filter = new DataCollectionFilter(instance, "Default rule",
                    "Rule created when the server was imported");
                filter.CreateFilter(repository.ConnectionString, connectionName);
            }
            catch (Exception ex)
            {
                logX.loggerX.Error(ex.Message);
                throw;
            }

        }

        private static void AddJobToRepository(Repository repository, ScheduleJob.ScheduleData scheduleData, string connectionName, string serverName)
        {
            try
            {
                scheduleData.SetDefaults();
                Guid jobID = ScheduleJob.AddJob(repository.ConnectionString,
                    connectionName,
                    repository.Instance, scheduleData, false);

                // Update Registered Server with new jobID 
                repository.GetServer(serverName).SetJobId(jobID);
            }
            catch (Exception ex)
            {
                logX.loggerX.Error(ex.Message);
                throw;
            }
        }


        private static bool CheckServerAccess(ImportItem importItem, string machine, out string errorMsg)
        {
            using (logX.loggerX.InfoCall())
            {
                try
                {
                    var winUser = importItem.UseSameCredentials ? importItem.UserName : importItem.WindowsUserName;
                    var winUserPassword = importItem.UseSameCredentials
                        ? importItem.Password
                        : importItem.WindowsUserPassword;

                    Server.ServerAccess sa = Server.CheckServerAccess(machine, winUser, winUserPassword,
                        out errorMsg);
                    return sa == Server.ServerAccess.OK;
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                    logX.loggerX.Error(ex.Message);
                    return false;
                }
            }
        }
        private static WindowsImpersonationContext Impersonate(string userName, string password)
        {
            using (logX.loggerX.InfoCall())
            {
                try
                {
                    WindowsIdentity wi =
                        Impersonation.GetCurrentIdentity(userName, password);
                    return wi.Impersonate();
                }
                catch (Exception ex)
                {
                    logX.loggerX.Error(ex.Message);
                    return null;
                }
            }
        }


        private static void UndoImpersonation(WindowsImpersonationContext context)
        {
            using (logX.loggerX.InfoCall())
            {
                if (context != null)
                {
                    context.Undo();
                    context.Dispose();
                }
            }
        }

        private static bool IsWindowsCredentialsValid(ImportItem importItem)
        {
            string domain, user;

            Path.SplitSamPath(importItem.UserName, out domain, out user);
            if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(user)) return false;
            return true;
        }

        private static bool ValidateCredentials(ImportItem importItem, out string sqlServerVersion, out string machine,
            out string instance, out string fullName, out string errorMsg)
        {
            using (logX.loggerX.InfoCall())
            {
                WindowsImpersonationContext impersonationContext = null;
                string login = string.Empty;
                string password = string.Empty;
                errorMsg = string.Empty;
                try
                {
                    if (importItem.AuthType == SqlServerAuthenticationType.SqlServerAuthentication)
                    {
                        login = importItem.UserName;
                        password = importItem.Password;
                    }
                    else if (IsWindowsCredentialsValid(importItem))
                        impersonationContext = Impersonate(importItem.UserName, importItem.Password);


                    SqlServer.GetSqlServerProperties(importItem.ServerName, login, password,
                        out sqlServerVersion, out machine, out instance, out fullName);
                }
                catch (Exception ex)
                {
                    logX.loggerX.Error(ex.Message);
                    errorMsg = ex.Message;
                    sqlServerVersion = string.Empty;
                    machine = Path.GetComputerFromSQLServerInstance(importItem.ServerName);
                    instance = Path.GetInstanceFromSQLServerInstance(importItem.ServerName);
                    fullName = importItem.ServerName.Trim().ToUpperInvariant();
                    return false;
                }
                finally
                {
                    if (importItem.AuthType == SqlServerAuthenticationType.WindowsAuthentication)
                    {
                        UndoImpersonation(impersonationContext);
                    }
                }
                return true;
            }
        }
    }
}
