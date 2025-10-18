using EvidenceSupportTool.Models;
using System.Collections.Generic;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// エビデンスの抽出に関する具体的なファイル操作の契約を定義します。
    /// </summary>
    public interface IEvidenceExtractionService
    {
        /// <summary>
        /// 指定されたパスに、監視対象のスナップショットを作成します。
        /// </summary>
        /// <param name="snapshotPath">スナップショットを保存する親フォルダのパス。</param>
        /// <param name="targets">監視対象のリスト。</param>
        void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets);

        /// <summary>
        /// 2つのスナップショットを比較し、差分を指定されたパスに保存します。
        /// </summary>
        /// <param name="snapshot1Path">スナップショット1の親フォルダパス。</param>
        /// <param name="snapshot2Path">スナップショット2の親フォルダパス。</param>
        /// <param name="evidencePath">差分ファイルの保存先親フォルダパス。</param>
        /// <returns>差分が検出された場合はtrue、それ以外の場合はfalse。</returns>
        bool ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath);
    }
}