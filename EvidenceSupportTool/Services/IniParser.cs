using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// INIファイルを解析する責務を持つヘルパークラスです。
    /// 特定のモデル（AppSettingsなど）には依存せず、汎用的なIniDataオブジェクトを生成します。
    /// </summary>
    public class IniParser
    {
        private readonly string _filePath;

        public IniParser(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// INIファイルを解析し、IniDataオブジェクトを返します。
        /// </summary>
        /// <returns>解析されたINIデータ</returns>
        /// <exception cref="FileNotFoundException">指定されたINIファイルが見つからない場合</exception>
        public IniData Parse()
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("INI file not found.", _filePath);
            }

            var iniData = new IniData();
            Dictionary<string, string>? currentSection = null;
            string? currentSectionName = null;

            foreach (var line in File.ReadAllLines(_filePath))
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue; // Skip empty lines and comments
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSectionName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    iniData.Sections[currentSectionName] = currentSection;
                    continue;
                }

                if (currentSection != null)
                {
                    var parts = trimmedLine.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        currentSection[key] = value;
                    }
                }
            }

            return iniData;
        }
    }
}
