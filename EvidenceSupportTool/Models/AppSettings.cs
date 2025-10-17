namespace EvidenceSupportTool.Models
{
    /// <summary>
    /// setting.iniの[Settings]セクションに対応するデータモデルです。
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// エビデンスの保存先パス
        /// </summary>
        public required string EvidenceSavePath { get; init; }

        /// <summary>
        /// スナップショットを保持するかどうか
        /// </summary>
        public bool KeepSnapshot { get; init; }
    }
}
