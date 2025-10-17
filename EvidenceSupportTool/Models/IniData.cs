using System.Collections.Generic;

namespace EvidenceSupportTool.Models
{
    /// <summary>
    /// IniParserによってパースされたINIファイルの生データを保持するデータ転送オブジェクト(DTO)です。
    /// </summary>
    public class IniData
    {
        /// <summary>
        /// セクション名をキーとし、そのセクション内のキーと値の辞書を値として持つ、ネストされた辞書構造です。
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Sections { get; } = new(System.StringComparer.OrdinalIgnoreCase);
    }
}
