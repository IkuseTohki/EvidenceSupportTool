using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ファイルやプロセスの監視を行うサービスの実装です。
    /// </summary>
    public class MonitoringService : IMonitoringService
    {
        private readonly IConfigService _configService;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IEvidenceExtractionService _evidenceExtractionService;
        private readonly List<MonitoringTarget> _monitoringTargets;
        private bool _isMonitoringActive;
        private string _currentEvidenceFolderPath = string.Empty;

        /// <summary>
        /// ステータスの変更を通知するイベントです。
        /// </summary>
        public event Action<string> StatusChanged;

        /// <summary>
        /// MonitoringServiceの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="configService">設定情報を提供するサービス。</param>
        /// <param name="userInteractionService">ユーザーとの対話（UI通知）を提供するサービス。</param>
        /// <param name="evidenceExtractionService">エビデンス抽出処理を提供するサービス。</param>
        public MonitoringService(
            IConfigService configService, 
            IUserInteractionService userInteractionService,
            IEvidenceExtractionService evidenceExtractionService)
        {
            _configService = configService;
            _userInteractionService = userInteractionService;
            _evidenceExtractionService = evidenceExtractionService;
            _monitoringTargets = new List<MonitoringTarget>();
            _isMonitoringActive = false;
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
        /// <returns>監視対象の読み取り専用リスト。</returns>
        public IReadOnlyList<MonitoringTarget> GetMonitoringTargets()
        {
            return _monitoringTargets.AsReadOnly();
        }

        /// <summary>
        /// 監視プロセスを開始します。
        /// </summary>
        public void Start()
        {
            if (_isMonitoringActive)
            {
                return;
            }

            _isMonitoringActive = true;
            _monitoringTargets.Clear();

            AppSettings appSettings = _configService.GetAppSettings();
            IEnumerable<MonitoringTarget> initialTargets = _configService.GetMonitoringTargets();
            _monitoringTargets.AddRange(initialTargets);

            // タイムスタンプ付きのフォルダパスを生成
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentEvidenceFolderPath = Path.Combine(appSettings.EvidenceSavePath, timestamp);

            // snapshot1のパスを構築
            string snapshot1Path = Path.Combine(_currentEvidenceFolderPath, "snapshot1");

            // スナップショット作成を委譲
            _evidenceExtractionService.CreateSnapshot(snapshot1Path, _monitoringTargets);

            OnStatusChanged("監視を開始しました。");
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
        /// 監視プロセスを停止します。
        /// </summary>
        public void Stop()
        {
            if (!_isMonitoringActive)
            {
                return;
            }

            _isMonitoringActive = false;

            // TODO: 代替案に基づき、snapshot2フォルダにファイルをコピーし、
            // snapshot1フォルダと比較してevidenceを作成する処理を実装

            // 仮実装: 差分がなかったことにして通知する
            _userInteractionService.ShowMessage("差分はありませんでした。");

            OnStatusChanged("監視を停止しました。");
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

        /// <summary>
        /// StatusChangedイベントを発行します。
        /// </summary>
        /// <param name="status">通知するステータスメッセージ。</param>
        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(status);
        }
    }
}