using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO; // Keep this using
using System.Linq; // Add this using

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IConfigServiceの実装クラスです。
    /// setting.iniファイルを実際にパースして、AppSettingsとMonitoringTargetのオブジェクトを生成します。
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly IniParser _parser;
        private readonly string _settingsFilePath;

        public ConfigService(IniParser parser, string settingsFilePath = "setting.ini")
        {
            _parser = parser;
            _settingsFilePath = settingsFilePath;
        }

        public AppSettings GetAppSettings()
        {
            var iniData = _parser.Parse(_settingsFilePath);

            if (!iniData.Sections.TryGetValue("Settings", out var settingsSection))
            {
                throw new InvalidOperationException("[Settings] section not found in the INI file.");
            }

            string? evidenceSavePath = null;
            bool? keepSnapshot = null;

            if (settingsSection.TryGetValue("EvidenceSavePath", out var pathValue))
            {
                evidenceSavePath = pathValue;
            }

            if (settingsSection.TryGetValue("KeepSnapshot", out var snapshotValue))
            {
                if (bool.TryParse(snapshotValue, out var result))
                {
                    keepSnapshot = result;
                }
            }

            if (evidenceSavePath == null || keepSnapshot == null)
            {
                throw new InvalidOperationException("[Settings] section is incomplete or invalid in the INI file.");
            }

            return new AppSettings
            {
                EvidenceSavePath = evidenceSavePath,
                KeepSnapshot = keepSnapshot.Value
            };
        }

        public IEnumerable<MonitoringTarget> GetMonitoringTargets()
        {
            var iniData = _parser.Parse(_settingsFilePath);

            if (!iniData.Sections.TryGetValue("Targets", out var targetsSection))
            {
                // If no [Targets] section, return an empty list
                return Enumerable.Empty<MonitoringTarget>();
            }

            var targets = new List<MonitoringTarget>();
            foreach (var entry in targetsSection)
            {
                targets.Add(new MonitoringTarget
                {
                    Name = entry.Key,
                    PathPattern = entry.Value
                });
            }

            return targets;
        }
    }
}

