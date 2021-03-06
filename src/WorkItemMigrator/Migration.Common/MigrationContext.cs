﻿using Migration.WIContract;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Migration.Common
{
    public class MigrationContext
    {
        public static MigrationContext Instance
        {
            get;
            private set;
        }

        public string AttachmentsPath { get { return Path.Combine(MigrationWorkspace, "Attachments"); } }
        public string UserMappingPath { get { return Path.Combine(MigrationWorkspace, "users.txt"); } }
        public Dictionary<string, string> UserMapping { get; private set; } = new Dictionary<string, string>();

        public static MigrationContext Init(string workspacePath, LogLevel logLevel, bool forceFresh)
        {
            Instance = new MigrationContext(workspacePath);
            Instance.LogLevel = logLevel;
            Instance.ForceFresh = forceFresh;

            Logger.Init(Instance);
            Instance.Journal = Journal.Init(Instance);
            Instance.Provider = new WiItemProvider(Instance.MigrationWorkspace);

            if (!Directory.Exists(Instance.AttachmentsPath))
                Directory.CreateDirectory(Instance.AttachmentsPath);

            return Instance;
        }
        private MigrationContext(string workspacePath)
        {
            MigrationWorkspace = workspacePath;
            ParseUserMappings(UserMappingPath);
        }

        private void ParseUserMappings(string userMappingPath)
        {
            if (!File.Exists(userMappingPath))
                return;

            string[] userMappings = File.ReadAllLines(userMappingPath);
            foreach (var userMapping in userMappings.Select(um => um.Split('=')))
            {
                string jiraUser = userMapping[0].Trim();
                string wiUser = userMapping[1].Trim();

                UserMapping.Add(jiraUser, wiUser);
            }
        }

        public string MigrationWorkspace { get; internal set; }
        public LogLevel LogLevel { get; internal set; }
        public bool ForceFresh { get; internal set; }

        public Journal Journal { get; internal set; }

        public WiItemProvider Provider { get; private set; }

        public WiItem GetItem(string originId)
        {
            var item =  this.Provider.Load(originId);
            item.WiId = Journal.GetMigratedId(originId);
            foreach (var link in item.Revisions.SelectMany(r => r.Links))
            {
                link.SourceOriginId = item.OriginId;
                link.SourceWiId = Journal.GetMigratedId(originId);
            }

            return item;
        }
        public IEnumerable<WiItem> EnumerateAllItems()
        {
            foreach (WiItem item in this.Provider.EnumerateAllItems())
            {
                item.WiId = Journal.GetMigratedId(item.OriginId);
                yield return item;
            } 
        }
    }
}
