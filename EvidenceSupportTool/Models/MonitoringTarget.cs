namespace EvidenceSupportTool.Models
{
    /// <summary>
    /// setting.iniの[Targets]セクションの個々のエントリに対応するデータモデルです。
    /// </summary>
    public class MonitoringTarget
    {
        /// <summary>
        /// ターゲット名（出力フォルダ名）
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 監視対象のパス（ワイルドカードや日付フォーマットを含む）
        /// </summary>
        public required string PathPattern { get; init; }
    }
}
