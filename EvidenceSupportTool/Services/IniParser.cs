using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvidenceSupportTool.Models;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// INIファイルを解析するヘルパークラスです。
    /// </summary>
    public class IniParser
    {
        /// <summary>
        /// 指定されたINIファイルを解析し、IniDataオブジェクトを返します。
        /// </summary>
        /// <param name="filePath">解析するINIファイルのパス。</param>
        /// <returns>解析されたINIデータを含むIniDataオブジェクト。</returns>
        /// <exception cref="FileNotFoundException">指定されたファイルが見つからない場合にスローされます。</exception>
        public IniData Parse(string filePath)
        {
            var iniData = new IniData();
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"設定ファイルが見つかりません: {filePath}");
            }

            string currentSection = string.Empty;
            foreach (string line in File.ReadAllLines(filePath))
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue; // 空行またはコメント行はスキップ
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    iniData.Sections[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != string.Empty)
                {
                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();
                        string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                        iniData.Sections[currentSection][key] = value;
                    }
                }
            }
            return iniData;
        }
    }
}