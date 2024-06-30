﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Updater;
using SharpDX;
using static ORTS.Common.SystemInfo;
using static ORTS.NotificationPage;

namespace ORTS
{
    class NotificationManager
    {
        public bool ArePagesVisible = false;
        // New notifications are those with a date after the NotificationsReadDate.
        // Notifications are listed in reverse date order, with the newest one at the front.
        // We don't track the reading of each notification but set the NewNotificationCount = 0 after the last of the new ones has been read.
        public int NewPageCount = 1;
        public int LastPageViewed = 0;
        public int Index = 0;

        public Notifications Notifications;
        public List<NotificationPage> PageList = new List<NotificationPage>();
        private Exception Error;
        private Dictionary<string, string> ParameterDictionary;

        private MainForm MainForm; // Needed so we can add controls to the NotificationPage
        private UpdateManager UpdateManager;
        private UserSettings Settings;

        public NotificationManager(MainForm mainForm, UpdateManager updateManager, UserSettings settings) 
        { 
            MainForm = mainForm;
            this.UpdateManager = updateManager;
            this.Settings = settings;
        }

        // Make this a background task
        public void CheckNotifications()
        {
            try
            {
                Error = null;
                Notifications = GetNotifications();
                DropUnusedUpdateNotifications();
                ParameterDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // To support testing, add any overriding values to the ValueDictionary
                GetOverrideParameters()?.ParameterValueList.ForEach(i => ParameterDictionary.Add(i.Parameter, i.Value));
                LogOverrideParameters();

                ReplaceParameters();
                LogParameters();

                PopulatePageList();
                ArePagesVisible = false;
            }
            catch (WebException ex)
            {
                Error = ex;
            }
        }

        static bool Log = false;
        static string LogFile = "notifications_trial_log.txt";

        public Notifications GetNotifications()
        {
            string notificationsSerial;
            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications tests first for a local file notifications_trial.json
            // and uses that if present, else it uses the remote file.

            var filename = @"notifications_trial.json";
            if (System.IO.File.Exists(filename))
            {
                // Input from local file into a string
                notificationsSerial = System.IO.File.ReadAllText(filename);

                // Turn on logging
                Log = true;
            }
            else
            {
                // Input from remote file into a string
                notificationsSerial = GetRemoteJson();
            }

            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jsonInput = JsonConvert.DeserializeObject<Notifications>(notificationsSerial, jsonSettings);

            return jsonInput;
        }

        /// <summary>
        /// Fetch the Notifications from https://static.openrails.org/api/notifications/menu.json 
        /// This file is copied hourly from Github openrails/notifications/
        /// </summary>
        private string GetRemoteJson()
        {
            var client = new WebClient()
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
                Encoding = Encoding.UTF8,
            };
            // Helpful to supply server with data for its log file.
            client.Headers[HttpRequestHeader.UserAgent] = $"{System.Windows.Forms.Application.ProductName}/{VersionInfo.VersionOrBuild}";

            return client.DownloadString(new Uri("https://wepp.co.uk/openrails/notifications/menu.json"));
        }

        public void PopulatePageList()
        {
            SetUpdateNotificationPage();

            var NotificationPage = PageList.LastOrDefault();
            new NTextControl(NotificationPage, "").Add();
            new NTextControl(NotificationPage, "(Toggle icon to hide these notifications.)").Add();
        }

        /// <summary>
        /// Ultimately there will be a list of notifications downloaded from https://static.openrails.org/api/notifications/menu.json .
        /// Until then, there is a single notification announcing either that a new update is available or the installation is up to date.
        /// </summary>
        void SetUpdateNotificationPage()
        {
            MainForm.UpdateNotificationPageAlert();
            PageList.Clear();
            var page = MainForm.CreateNotificationPage(Notifications);

            if (UpdateManager.LastCheckError != null || Error != null)
            {
                NewPageCount = 0;
                var message = (UpdateManager.LastCheckError != null)
                    ? UpdateManager.LastCheckError.Message
                    : Error.Message;

                // Reports notifications are not available.
                var channelName = UpdateManager.ChannelName == "" ? "None" : UpdateManager.ChannelName;
                var today = DateTime.Now.Date;
                new NTitleControl(page, 1, 1, $"{today:dd-MMM-yy}", "Notifications are not available").Add();
                new NRecordControl(page, "Update mode", 140, channelName).Add();
                new NRecordControl(page, "Installed version", 140, VersionInfo.VersionOrBuild).Add();

                new NHeadingControl(page, "Notifications are not available", "red").Add();
                new NTextControl(page, $"Error: {message}").Add();
                new NTextControl(page, "Is your Internet connected?").Add();

                new NRetryControl(page, "Retry", 140, "Try again to fetch notifications", MainForm).Add();
                PageList.Add(page);
                return;
            }

            NewPageCount = 1;
            var list = Notifications.NotificationList;
            var n = list[Index];
            LogNotification(n);

            var skipPage = false;
            new NTitleControl(page, Index + 1, list.Count, n.Date, n.Title).Add();

            // Check constraints for the MetList.
            //var failingCheck = (n.Met.ItemList?.Count > 0 && n.Met.CheckIdList?.Count > 0) 
            //    ? CheckConstraints(n) 
            //    : null;

            // If any check fails then its UnmetList is added to the page, otherwise the MetList is added.
            //n.PrefixItemList?.ForEach(item => AddItemToPage(page, item));
            //if (failingCheck == null)
            //{
            //    n.Met.ItemList?.ForEach(item => AddItemToPage(page, item));
            //}
            //else
            //{
            //    if (failingCheck.UnmetItemList == null) // Omit this section to skip the notification entirely.
            //    {
            //        // Don't skip if there is only one notification.
            //        if (list.Count > 1) skipPage = true;
            //    }
            //    else
            //    {
            //        failingCheck.UnmetItemList?.ForEach(item => AddItemToPage(page, item));
            //    }
            //}
            //n.SuffixItemList?.ForEach(item => AddItemToPage(page, item));
            n.ItemList?.ForEach(item => AddItemToPage(page, item));
            if (skipPage == false) PageList.Add(page);
        }

        /// <summary>
        /// CheckConstraints() checks the constraints in sequence, but all parts are optional.
        /// If all the constraints in any Includes are met, then the whole check is met.
        /// If all the constraints in any Excludes are met, then the whole check is not met.
        /// Returns null if the whole check is met else returns the check that failed.
        /// </summary>
        /// <param name="n"></param>
        private Check CheckConstraints(Notification n)
        {
            Check failingCheck = null;
            foreach (var checkName in n.CheckIdList) // CheckIdList is optional
            {
                LogChecks(checkName);

                // Find the matching check
                var check = Notifications.CheckList.Where(c => c.Id == checkName).FirstOrDefault();
                if (check != null && check.AnyOfList.Count() > 0)
                {
                    foreach(var anyOf in check.AnyOfList)
                    {
                        //if (anyOf is Excludes)
                        //{
                        //    if (CheckAllMatch(anyOf.AllOfList) == true) return check; // immediate fail so quit
                        //}
                        //if (anyOf is Includes)
                        //{
                        //    if (CheckAllMatch(anyOf.AllOfList) == false) failingCheck = check; // fail but continue testing other Includes
                        //}
                    }
                }
            }
            return failingCheck;
        }

        private void AddItemToPage(NotificationPage page, Item item)
        {
            if (item is Record record)
            {
                new NRecordControl(page, item.Label, item.Indent, record.Value).Add();
            }
            else if (item is Link link)
            {
                new NLinkControl(page, item.Label, item.Indent, link.Value, MainForm, link.Url).Add();
            }
            else if (item is Update update)
            {
                new NUpdateControl(page, item.Label, item.Indent, update.Value, MainForm).Add();
            }
            else if (item is Heading heading)
            {
                new NHeadingControl(page, item.Label, heading.Color).Add();
            }
            else if (item is Text text)
            {
                new NTextControl(page, item.Label, text.Color).Add();
            }
            else if (item is Item item2)
            {
                new NTextControl(page, item.Label).Add();
            }
        }

        private bool CheckAllMatch(List<Criteria> criteriaList)
        {
            foreach (var c in criteriaList)
            {
                if (c is Contains) // other criteria might be added such as NoLessThan and NoMoreThan.
                {
                    if (CheckContains(c, true) == false) return false;
                }
                if (c is NotContains)
                {
                    if (CheckContains(c, false) == false) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if a match is found and sense = true. For NotContains, use sense = false.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="sense"></param>
        /// <returns></returns>
        private bool CheckContains(Criteria criteria, bool sense)
        {
            // If Property was a parameter, then use the expansion
            var content = ParameterDictionary.ContainsKey(criteria.Property)
                ? ParameterDictionary[criteria.Property]
                : criteria.Property;

            var result = content.IndexOf(criteria.Value, StringComparison.OrdinalIgnoreCase) > -1 == sense;
            LogCheckContains(criteria.Value, sense, content, result);
            return result;
        }

        /// <summary>
        /// Drop any notifications for the channel not selected
        /// </summary>
        /// <param name="updateManager"></param>
        public void DropUnusedUpdateNotifications()
        {
            var updateModeSetting = UpdateManager.ChannelName.ToLower();
            foreach (var n in Notifications.NotificationList)
            {
                if (n.UpdateMode == null)   // Skip notifications which are not updates
                    continue;

                var lowerUpdateMode = n.UpdateMode.ToLower();

                // If setting == "none", then keep just one update notification, e.g. the stable one
                if (updateModeSetting == "" && lowerUpdateMode == "stable")
                    continue;

                // Mark unused updates for deletion outside loop
                //n.ToDelete = lowerUpdateMode != updateModeSetting;
            }
            //Notifications.NotificationList.RemoveAll(n => n.ToDelete);
        }

        public void ReplaceParameters()
        {
            foreach (var n in Notifications.NotificationList)
            {
                n.Title = ReplaceParameter(n.Title);
                n.Date = ReplaceParameter(n.Date);
                //n.PrefixItemList?.ForEach(item => ReplaceItemParameter(item));
                //n.Met.ItemList?.ForEach(item => ReplaceItemParameter(item));
                //n.SuffixItemList?.ForEach(item => ReplaceItemParameter(item));
                n.ItemList?.ForEach(item => ReplaceItemParameter(item));
            }
            foreach (var list in Notifications.CheckList)
            {
                foreach(var c in list?.AnyOfList)
                {
                    c?.AllOfList.ForEach(criteria => ReplaceCriteriaPropertyParameter(criteria));
                    c?.AllOfList.ForEach(criteria => ReplaceCriteriaValueParameter(criteria));
                }
            }
        }

        private void ReplaceItemParameter(Item item)
        {
            if (item is Record record)
                record.Value = ReplaceParameter(record.Value);
            if (item is Link link)
                link.Value = ReplaceParameter(link.Value);
            if (item is Update update)
                update.Value = ReplaceParameter(update.Value);
        }

        /// <summary>
        /// If Property is a parameter, remove {{..}} and add it and its replacement to the dictionary.
        /// </summary>
        /// <param name="criteria"></param>
        private void ReplaceCriteriaPropertyParameter(Criteria criteria)
        {
            if (ContainsParameter(criteria.Property))
            {
                criteria.Property = ReplaceParameter(criteria.Property);
            }
        }

        private void ReplaceCriteriaValueParameter(Criteria criteria)
        {
            criteria.Value = ReplaceParameter(criteria.Value);
        }

        private string ReplaceParameter(string field)
        {
            if (ContainsParameter(field) == false) return field;

            var parameterArray = field.Split('{', '}'); // 5 elements: prefix, "", target, "", suffix
            var target = parameterArray[2];
            var lowerCaseTarget = parameterArray[2].ToLower();
            var replacement = parameterArray[2]; // Default is original text

            // If found in dictionary, then use that else extract it from program
            if (ParameterDictionary.ContainsKey(lowerCaseTarget))
            {
                replacement = ParameterDictionary[lowerCaseTarget];
            }
            else
            {
                switch (lowerCaseTarget)
                {
                    // Using "none" instead of "" so that records are readable.
                    case "update_mode":
                        replacement = (UpdateManager.ChannelName == "")
                            ? "none"
                            : UpdateManager.ChannelName;
                        break;
                    case "latest_version":
                        replacement = UpdateManager.LastUpdate == null
                            || UpdateManager.ChannelName == ""
                            ? "none"
                            : UpdateManager.LastUpdate.Version;
                        break;
                    case "release_date":
                        replacement = UpdateManager.LastUpdate == null
                            ? "none"
                            : $"{UpdateManager.LastUpdate.Date:dd-MMM-yy}";
                        break;

                    case "installed_version":
                        replacement = SystemInfo.Application.Version;
                        break;
                    case "runtime":
                        replacement = Runtime.ToString();
                        break;
                    case "system":
                        replacement = SystemInfo.OperatingSystem.ToString();
                        break;
                    case "memory":
                        replacement = Direct3DFeatureLevels.ToString();
                        break;
                    case "cpu":
                        replacement = "";
                        foreach (var cpu in CPUs)
                        {
                            replacement += $", {cpu.Name}";
                        }
                        break;
                    case "gpu":
                        replacement = "";
                        foreach (var gpu in GPUs)
                        {
                            replacement += $", {gpu.Name}";
                        }
                        break;
                    case "direct3d":
                        replacement = string.Join(",", Direct3DFeatureLevels);
                        break;

                    case "installed_routes":
                        replacement = GetInstalledRoutes();
                        break;

                    default:
                        var propertyValue = GetSetting(target);
                        replacement = (propertyValue == "")
                            ? field     // strings that are not recognised are not replaced.
                            : propertyValue.ToLower().Replace("false", "off").Replace("true", "on");
                        break;
                }
                ParameterDictionary.Add(lowerCaseTarget, replacement);
            }

            return parameterArray[0] + replacement + parameterArray[4];
        }

        private bool ContainsParameter(string field)
        {
            if (field.Contains("{{") == false) return false;
            if (field.Contains("}}") == false) return false;
            return true;
        }

        /// <summary>
        /// Gets the property value of a UserSetting. The setting must match case as in "Setting.SimpleControlPhysics".
        /// </summary>
        /// <param name="settingText"></param>
        /// <returns></returns>
        string GetSetting(string settingText)
        {
            var nameArray = settingText.Split('.'); // 2 elements: "Settings, "<property>", e.g. "SimpleControlPhysics" 
            if (nameArray[0] == "Settings" && nameArray.Length == 2)
            {
                return Settings.GetType().GetProperty(nameArray[1])?.GetValue(Settings).ToString() ?? "";
            }
            return "";
        }

        private string GetInstalledRoutes()
        {
            var installedRouteList = "";
            var contentDictionary = Settings.Folders.Folders;
            foreach (var folder in contentDictionary)
            {
                var path = folder.Value + @"\ROUTES\";
                if (Directory.Exists(path))
                {
                    foreach (var routePath in Directory.GetDirectories(path))
                    {
                        // Extract the last folder in the path - the route folder name, e.g. "SCE"
                        var routeName = System.IO.Path.GetFileName(routePath).ToLower();
                        installedRouteList += routeName + ",";
                    }
                }
            }
            return installedRouteList;
        }

        public OverrideParameterList GetOverrideParameters()
        {
            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications tests first for a local file notifications_override_values.json
            // and uses that if present to override the current program values, else it extracts these from the program.

            var filename = @"notifications_trial_parameters.json";
            if (System.IO.File.Exists(filename))
            {
                // Input from local file into a string
                var overrideParametersSerial = System.IO.File.ReadAllText(filename);

                var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                var jsonInput = JsonConvert.DeserializeObject<OverrideParameterList>(overrideParametersSerial, jsonSettings);

                return jsonInput;
            }
            else
                return null;
        }

        public void LogOverrideParameters()
        {
            if (Log == false) return;

            if (File.Exists(LogFile)) File.Delete(LogFile);

            using (StreamWriter sw = File.CreateText(LogFile))
            {
                sw.WriteLine("Parameters overridden:");
                foreach (var p in ParameterDictionary)
                {
                    sw.WriteLine($"{p.Key} = {p.Value}");
                }
                sw.WriteLine();
            }
        }

        public void LogParameters()
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile))
            {
                sw.WriteLine("Parameters used:");
                foreach (var p in ParameterDictionary)
                {
                    sw.WriteLine($"{p.Key} = {p.Value}");
                }
                sw.WriteLine();
            }
        }

        public void LogNotification(Notification n)
        {
            AppendToLog($"Notification: {n.Title}");
        }
        public void LogChecks(string checkName)
        {
            AppendToLog($"CheckId: {checkName}");
        }
        public void LogCheckContains(string value, bool sense, string content, bool result)
        {
            var negation = sense ? "" : "NOT ";
            AppendToLog($"Check: '{value}' {negation}contained in '{content}' = {result}");
        }

        public void AppendToLog(string record)
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile))
            {
                sw.WriteLine(record);
            }
        }
    }
}
