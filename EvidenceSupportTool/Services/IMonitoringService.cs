// IMonitoringService.cs

using EvidenceSupportTool.Models;
using System.Collections.Generic;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ファイルやプロセスの監視を行うサービスのインターフェースです。
    /// </summary>
    public interface IMonitoringService
    {
        /// <summary>
        /// 監視対象を追加します。
        /// </summary>
        /// <param name="target">追加する監視対象。</param>
        void AddMonitoringTarget(MonitoringTarget target);

        /// <summary>
        /// 現在登録されているすべての監視対象を取得します。
        /// </summary>
        /// <returns>監視対象のリスト。</returns>
        IReadOnlyList<MonitoringTarget> GetMonitoringTargets();

        /// <summary>
        /// 監視を開始します。
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// 監視が現在アクティブであるかどうかを示します。
        /// </summary>
        /// <returns>監視がアクティブな場合はtrue、それ以外の場合はfalse。</returns>
        bool IsMonitoringActive();

        /// <summary>
        /// 監視を停止します。
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 指定された名前の監視対象を削除します。
        /// </summary>
        /// <param name="name">削除する監視対象の名前。</param>
        /// <returns>削除に成功した場合はtrue、それ以外の場合はfalse。</returns>
        bool RemoveMonitoringTarget(string name);
    }
}
