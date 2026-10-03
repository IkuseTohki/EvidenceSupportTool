using EvidenceSupportTool.Models;
using System.Collections.Generic;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// 設定情報を取得するための契約を定義します。
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// アプリケーション設定を取得します。
        /// </summary>
        /// <returns>AppSettingsオブジェクト</returns>
        AppSettings GetAppSettings();

        /// <summary>
        /// 監視対象のリストを取得します。
        /// </summary>
        /// <returns>MonitoringTargetのコレクション</returns>
        IEnumerable<MonitoringTarget> GetMonitoringTargets();

        /// <summary>
        /// コピー収集対象のリストを取得します。
        /// </summary>
        /// <returns>CollectTargetのコレクション</returns>
        IEnumerable<CollectTarget> GetCollectTargets();

        /// <summary>
        /// 指定されたパスの設定ファイルを読み込み、設定ファイルパスを更新します。
        /// </summary>
        /// <param name="filePath">設定ファイルのパス</param>
        void LoadSettings(string filePath);
    }
}
