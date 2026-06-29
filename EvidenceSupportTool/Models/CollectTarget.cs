namespace EvidenceSupportTool.Models
{
    /// <summary>
    /// setting.iniの[CollectTargets]セクションの個々のエントリに対応するデータモデルです。
    /// 差分抽出ではなく、ファイルをそのままコピーして収集する対象を表します。
    /// </summary>
    public class CollectTarget
    {
        /// <summary>
        /// ターゲット名（collect/ 直下の出力フォルダ名）
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 収集対象のパス（ワイルドカードや日付フォーマットを含む）
        /// </summary>
        public required string PathPattern { get; init; }
    }
}
