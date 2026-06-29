using EvidenceSupportTool.Models;
using System.Collections.Generic;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ログファイルをそのままコピーして収集する処理の契約を定義します。
    /// </summary>
    public interface ILogCollectionService
    {
        /// <summary>
        /// 指定された収集対象のファイルを collectPath へコピーします。
        /// パスパターン内の日付フォーマット（{YYYY}, {MM}, {DD}）は実行時の日付で解決されます。
        /// ワイルドカード（*）はファイル名検索に使用されます。
        /// </summary>
        /// <param name="collectPath">収集先のルートフォルダパス。</param>
        /// <param name="targets">収集対象のリスト。</param>
        void Collect(string collectPath, IEnumerable<CollectTarget> targets);
    }
}
