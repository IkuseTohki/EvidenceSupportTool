// MonitoringService.cs

using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.Linq; // For .RemoveAll()

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ファイルやプロセスの監視を行うサービスの実装です。
    /// </summary>
    public class MonitoringService : IMonitoringService
    {
        private readonly List<MonitoringTarget> _monitoringTargets;
        private bool _isMonitoringActive; // 監視がアクティブかどうかを示すフラグ

        public MonitoringService()
        {
            _monitoringTargets = new List<MonitoringTarget>();
            _isMonitoringActive = false; // 初期状態は非アクティブ
        }

        /// <summary>
        /// 監視対象を追加します。
        /// </summary>
        /// <param name="target">追加する監視対象。</param>
        public void AddMonitoringTarget(MonitoringTarget target)
        {
            _monitoringTargets.Add(target);
        }

        /// <summary>
        /// 現在登録されているすべての監視対象を取得します。
        /// </summary>
        /// <returns>監視対象のリスト。</returns>
        public IReadOnlyList<MonitoringTarget> GetMonitoringTargets()
        {
            return _monitoringTargets.AsReadOnly();
        }

        /// <summary>
        /// 監視を開始します。
        /// </summary>
        public void StartMonitoring()
        {
            _isMonitoringActive = true;
            // TODO: 実際の監視開始ロジックを実装
        }

        /// <summary>
        /// 監視が現在アクティブであるかどうかを示します。
        /// </summary>
        /// <returns>監視がアクティブな場合はtrue、それ以外の場合はfalse。</returns>
        public bool IsMonitoringActive()
        {
            return _isMonitoringActive;
        }

        /// <summary>
        /// 監視を停止します。
        /// </summary>
        public void StopMonitoring()
        {
            _isMonitoringActive = false;
            // TODO: 実際の監視停止ロジックを実装
        }

        /// <summary>
        /// 指定された名前の監視対象を削除します。
        /// </summary>
        /// <param name="name">削除する監視対象の名前。</param>
        /// <returns>削除に成功した場合はtrue、それ以外の場合はfalse。</returns>
        public bool RemoveMonitoringTarget(string name)
        {
            int removedCount = _monitoringTargets.RemoveAll(t => t.Name == name);
            return removedCount > 0;
        }
    }
}
