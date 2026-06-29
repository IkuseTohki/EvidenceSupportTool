using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvidenceSupportTool.Services;
using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static System.Guid;
using System;
using System.Text;

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class MonitoringServiceTests
    {
        // IConfigServiceのモッククラス
        private class MockConfigService : IConfigService
        {
            public AppSettings AppSettings { get; set; } = new AppSettings { EvidenceSavePath = "C:\\TempEvidence", KeepSnapshot = false };
            public IEnumerable<MonitoringTarget> MonitoringTargets { get; set; } = new List<MonitoringTarget>();
            public IEnumerable<CollectTarget> CollectTargets { get; set; } = new List<CollectTarget>();

            public AppSettings GetAppSettings() => new AppSettings 
            { 
                EvidenceSavePath = AppSettings.EvidenceSavePath, 
                KeepSnapshot = AppSettings.KeepSnapshot,
                AutoSaveScreenshots = AppSettings.AutoSaveScreenshots
            };
            public IEnumerable<MonitoringTarget> GetMonitoringTargets() => MonitoringTargets;
            public IEnumerable<CollectTarget> GetCollectTargets() => CollectTargets;

            public void SetKeepSnapshot(bool value)
            {
                AppSettings = new AppSettings { EvidenceSavePath = AppSettings.EvidenceSavePath, KeepSnapshot = value };
            }
        }

        // IUserInteractionServiceのモッククラス
        private class MockUserInteractionService : IUserInteractionService
        {
            public List<string> ShowMessageCalls { get; } = new List<string>();
            public List<string> ShowErrorCalls { get; } = new List<string>();

            public void ShowMessage(string message) => ShowMessageCalls.Add(message);
            public void ShowError(string message) => ShowErrorCalls.Add(message);
        }

        // IEvidenceExtractionServiceのモッククラス
        private class MockEvidenceExtractionService : IEvidenceExtractionService
        {
            public List<(string snapshotPath, IEnumerable<MonitoringTarget> targets)> CreateSnapshotCalls { get; } = new();
            public List<(string snapshot1Path, string snapshot2Path, string evidencePath, bool keepSnapshot)> ExtractEvidenceCalls { get; } = new();
            public bool ExtractEvidenceReturnValue { get; set; } = false;

            public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
            {
                CreateSnapshotCalls.Add((snapshotPath, targets));
            }

            public bool ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath, bool keepSnapshot)
            {
                ExtractEvidenceCalls.Add((snapshot1Path, snapshot2Path, evidencePath, keepSnapshot));
                return ExtractEvidenceReturnValue;
            }
        }

        // IScreenshotServiceのモッククラス
        private class MockScreenshotService : IScreenshotService
        {
            public List<string> StartCalls { get; } = new();
            public int StopCalls { get; private set; }

            public void Start(string savePath) => StartCalls.Add(savePath);
            public void Stop() => StopCalls++;
        }

        private MockConfigService _mockConfigService = null!;
        private MockUserInteractionService _mockUserInteractionService = null!;
        private MockEvidenceExtractionService _mockEvidenceExtractionService = null!;
        private MockScreenshotService _mockScreenshotService = null!;
        private List<string> _receivedStatuses = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfigService = new MockConfigService();
            _mockUserInteractionService = new MockUserInteractionService();
            _mockEvidenceExtractionService = new MockEvidenceExtractionService();
            _mockScreenshotService = new MockScreenshotService();
            _receivedStatuses = new List<string>();
        }

        [TestMethod]
        public void AddMonitoringTarget_ShouldAddTargetToList()
        {
            // テストの観点: 監視対象がリストに正しく追加されること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            var target = new MonitoringTarget { Name = "TestTarget", PathPattern = "C:\\test\\file.txt" };

            // Act
            monitoringService.AddMonitoringTarget(target);

            // Assert
            var targets = monitoringService.GetMonitoringTargets();
            Assert.IsNotNull(targets);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(target, targets.First());
        }

        [TestMethod]
        public void GetMonitoringTargets_ShouldReturnEmptyListInitially()
        {
            // テストの観点: 初期状態では、監視対象リストが空であること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);

            // Act
            var targets = monitoringService.GetMonitoringTargets();

            // Assert
            Assert.IsNotNull(targets);
            Assert.AreEqual(0, targets.Count);
        }

        [TestMethod]
        public void Start_ShouldSetIsMonitoringActiveToTrue()
        {
            // テストの観点: Startメソッド呼び出し後、監視状態がアクティブになること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            Assert.IsFalse(monitoringService.IsMonitoringActive());

            // Act
            monitoringService.Start();

            // Assert
            Assert.IsTrue(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void Stop_ShouldSetIsMonitoringActiveToFalse()
        {
            // テストの観点: Stopメソッド呼び出し後、監視状態が非アクティブになること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            monitoringService.Start();
            Assert.IsTrue(monitoringService.IsMonitoringActive());

            // Act
            monitoringService.Stop();

            // Assert
            Assert.IsFalse(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void IsMonitoringActive_ShouldReturnFalseInitially()
        {
            // テストの観点: 初期状態では、監視状態が非アクティブであること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);

            // Act & Assert
            Assert.IsFalse(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void RemoveMonitoringTarget_ShouldRemoveExistingTarget()
        {
            // テストの観点: 登録済みの監視対象が、名前を指定して正しく削除できること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            var target1 = new MonitoringTarget { Name = "Target1", PathPattern = "C:\\path1" };
            var target2 = new MonitoringTarget { Name = "Target2", PathPattern = "C:\\path2" };
            monitoringService.AddMonitoringTarget(target1);
            monitoringService.AddMonitoringTarget(target2);
            Assert.AreEqual(2, monitoringService.GetMonitoringTargets().Count);

            // Act
            bool result = monitoringService.RemoveMonitoringTarget("Target1");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, monitoringService.GetMonitoringTargets().Count);
            Assert.IsFalse(monitoringService.GetMonitoringTargets().Any(t => t.Name == "Target1"));
            Assert.IsTrue(monitoringService.GetMonitoringTargets().Any(t => t.Name == "Target2"));
        }

        [TestMethod]
        public void RemoveMonitoringTarget_ShouldReturnFalseForNonExistingTarget()
        {
            // テストの観点: 登録されていない名前を指定した場合、削除が失敗しfalseが返ること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            var target1 = new MonitoringTarget { Name = "Target1", PathPattern = "C:\\path1" };
            monitoringService.AddMonitoringTarget(target1);
            Assert.AreEqual(1, monitoringService.GetMonitoringTargets().Count);

            // Act
            bool result = monitoringService.RemoveMonitoringTarget("NonExistentTarget");

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, monitoringService.GetMonitoringTargets().Count);
            Assert.IsTrue(monitoringService.GetMonitoringTargets().Any(t => t.Name == "Target1"));
        }

        [TestMethod]
        public void StatusChanged_EventShouldBeRaisedOnStartAndStop()
        {
            // テストの観点: StartおよびStopメソッド呼び出し時に、StatusChangedイベントが適切なメッセージで発火すること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            monitoringService.StatusChanged += (status) => _receivedStatuses.Add(status);

            // Act & Assert for Start
            monitoringService.Start();
            Assert.IsTrue(_receivedStatuses.Contains("監視を開始しました。"));

            // Act & Assert for Stop
            monitoringService.Stop();
            Assert.IsTrue(_receivedStatuses.Contains("監視を停止しました。"));
        }

        [TestMethod]
        public void Start_ShouldLoadTargetsFromConfigService()
        {
            // テストの観点: Startメソッド呼び出し時に、ConfigServiceから設定された監視対象がロードされること。
            // Arrange
            var target1 = new MonitoringTarget { Name = "ConfigTarget1", PathPattern = "C:\\config\\path1" };
            var target2 = new MonitoringTarget { Name = "ConfigTarget2", PathPattern = "C:\\config\\path2" };
            _mockConfigService.MonitoringTargets = new List<MonitoringTarget> { target1, target2 };

            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);

            // Act
            monitoringService.Start();

            // Assert
            var loadedTargets = monitoringService.GetMonitoringTargets();
            Assert.IsNotNull(loadedTargets);
            Assert.AreEqual(2, loadedTargets.Count);
            Assert.IsTrue(loadedTargets.Any(t => t.Name == "ConfigTarget1"));
            Assert.IsTrue(loadedTargets.Any(t => t.Name == "ConfigTarget2"));

            // Clean up
            monitoringService.Stop();
        }

        [TestMethod]
        public void Stop_ShouldNotifyWhenNoChanges()
        {
            // テストの観点: ExtractEvidenceがfalseを返した場合（差分なし）、ユーザーに通知されること。
            // Arrange
            _mockEvidenceExtractionService.ExtractEvidenceReturnValue = false; // 差分なしをシミュレート
            _mockConfigService.SetKeepSnapshot(false); // Add this for testing keepSnapshot
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert
            Assert.AreEqual(1, _mockUserInteractionService.ShowMessageCalls.Count);
            Assert.AreEqual("差分はありませんでした。", _mockUserInteractionService.ShowMessageCalls[0]);
            Assert.AreEqual(1, _mockEvidenceExtractionService.ExtractEvidenceCalls.Count);
            var (_, _, _, keepSnapshot) = _mockEvidenceExtractionService.ExtractEvidenceCalls[0];
            Assert.IsFalse(keepSnapshot);
        }

        [TestMethod]
        public void Start_ShouldCreateEvidenceFolderAndCallCreateSnapshot()
        {
            // テストの観点: Start時にタイムスタンプ付きのフォルダパスを生成し、CreateSnapshotが適切な引数で呼ばれること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            var targets = new List<MonitoringTarget> { new MonitoringTarget { Name = "Test", PathPattern = "C:\\test.log" } };
            _mockConfigService.MonitoringTargets = targets;

            // Act
            monitoringService.Start();

            // Assert
            Assert.AreEqual(1, _mockEvidenceExtractionService.CreateSnapshotCalls.Count);
            var (snapshotPath, calledTargets) = _mockEvidenceExtractionService.CreateSnapshotCalls[0];

            // パスが期待通りか検証 (EvidenceSavePath と snapshot1 を含むか)
            StringAssert.Contains(snapshotPath, _mockConfigService.AppSettings.EvidenceSavePath);
            StringAssert.Contains(snapshotPath, "snapshot1");

            // タイムスタンプ部分の検証 (yyyyMMdd_HHmmss の形式に近いか)
            var dirPath = Path.GetDirectoryName(snapshotPath);
            Assert.IsNotNull(dirPath);
            var dirName = new DirectoryInfo(dirPath).Name;
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(dirName, "^\\d{8}_\\d{6}$"));

            // 渡されたターゲットが正しいか検証
            CollectionAssert.AreEqual(targets, calledTargets.ToList());
        }

        [TestMethod]
        public void Stop_ShouldCallCreateSnapshotAndExtractEvidence()
        {
            // テストの観点: Stop時にsnapshot2作成とevidence抽出が適切な引数で呼ばれること。
            // Arrange
            _mockConfigService.SetKeepSnapshot(true); // Add this for testing keepSnapshot
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            var targets = new List<MonitoringTarget> { new MonitoringTarget { Name = "Test", PathPattern = "C:\\test.log" } };
            _mockConfigService.MonitoringTargets = targets;
            monitoringService.Start(); // これで _currentEvidenceFolderPath が設定される

            // Act
            monitoringService.Stop();

            // Assert
            // 1. snapshot2が作成されることの検証
            Assert.AreEqual(2, _mockEvidenceExtractionService.CreateSnapshotCalls.Count); // Startで1回, Stopで1回
            var (snapshot2Path, _) = _mockEvidenceExtractionService.CreateSnapshotCalls[1];
            StringAssert.Contains(snapshot2Path, "snapshot2");

            // 2. evidenceが抽出されることの検証
            Assert.AreEqual(1, _mockEvidenceExtractionService.ExtractEvidenceCalls.Count);
            var (s1Path, s2Path, evPath, keepSnapshot) = _mockEvidenceExtractionService.ExtractEvidenceCalls[0]; // Modified
            StringAssert.Contains(s1Path, "snapshot1");
            StringAssert.Contains(s2Path, "snapshot2");
            StringAssert.Contains(evPath, "evidence");
            Assert.IsTrue(keepSnapshot); // New
        }

        [TestMethod]
        public void Stop_ShouldNotNotifyWhenChangesExist()
        {
            // テストの観点: ExtractEvidenceがtrueを返した場合（差分あり）、ユーザーに通知されないこと。
            // Arrange
            _mockEvidenceExtractionService.ExtractEvidenceReturnValue = true; // 差分ありをシミュレート
            _mockConfigService.SetKeepSnapshot(true); // Add this for testing keepSnapshot
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert
            Assert.AreEqual(0, _mockUserInteractionService.ShowMessageCalls.Count);
            Assert.AreEqual(1, _mockEvidenceExtractionService.ExtractEvidenceCalls.Count);
            var (_, _, _, keepSnapshot) = _mockEvidenceExtractionService.ExtractEvidenceCalls[0];
            Assert.IsTrue(keepSnapshot); // New
        }

        [TestMethod]
        public void Start_ShouldCallScreenshotService_WhenAutoSaveIsEnabled()
        {
            // テストの観点: 自動保存が有効な場合、監視開始時にScreenshotService.Startが呼ばれること。
            // Arrange
            _mockConfigService.AppSettings = new AppSettings 
            { 
                EvidenceSavePath = "C:\\TempEvidence", 
                KeepSnapshot = false,
                AutoSaveScreenshots = true 
            };
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);

            // Act
            monitoringService.Start();

            // Assert
            Assert.AreEqual(1, _mockScreenshotService.StartCalls.Count);
            StringAssert.Contains(_mockScreenshotService.StartCalls[0], "screenshots");
        }

        [TestMethod]
        public void Stop_ShouldCallScreenshotServiceStop_WhenAutoSaveIsEnabled()
        {
            // テストの観点: 自動保存が有効な場合、監視停止時にScreenshotService.Stopが呼ばれること。
            // Arrange
            _mockConfigService.AppSettings = new AppSettings 
            { 
                EvidenceSavePath = "C:\\TempEvidence", 
                KeepSnapshot = false,
                AutoSaveScreenshots = true 
            };
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);
            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert
            Assert.AreEqual(1, _mockScreenshotService.StopCalls);
        }

        [TestMethod]
        public void Start_ShouldNotCallScreenshotService_WhenAutoSaveIsDisabled()
        {
            // テストの観点: 自動保存が無効な場合、監視開始時にScreenshotService.Startが呼ばれないこと。
            // Arrange
            _mockConfigService.AppSettings = new AppSettings 
            { 
                EvidenceSavePath = "C:\\TempEvidence", 
                KeepSnapshot = false,
                AutoSaveScreenshots = false 
            };
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService, _mockScreenshotService);

            // Act
            monitoringService.Start();
            // Assert
            Assert.AreEqual(0, _mockScreenshotService.StartCalls.Count);
        }

        // ILogCollectionServiceのモッククラス
        private class MockLogCollectionService : ILogCollectionService
        {
            public List<(string collectPath, IEnumerable<CollectTarget> targets)> CollectCalls { get; } = new();

            public void Collect(string collectPath, IEnumerable<CollectTarget> targets)
            {
                CollectCalls.Add((collectPath, targets));
            }
        }

        /// <summary>
        /// テスト観点: Stop()呼び出し時に ILogCollectionService.Collect() が正しいパスで呼ばれること
        /// </summary>
        [TestMethod]
        public void Stop_ShouldCallLogCollectionService_WithCorrectPath()
        {
            // Arrange
            var evidenceSavePath = "C:\\TempEvidence";
            _mockConfigService.AppSettings = new AppSettings
            {
                EvidenceSavePath = evidenceSavePath,
                KeepSnapshot = false,
                AutoSaveScreenshots = false
            };
            var collectTarget = new CollectTarget { Name = "IisLog", PathPattern = "C:\\inetpub\\logs\\*.log" };
            _mockConfigService.CollectTargets = new[] { collectTarget };

            var mockLogCollectionService = new MockLogCollectionService();
            var monitoringService = new MonitoringService(
                _mockConfigService,
                _mockUserInteractionService,
                _mockEvidenceExtractionService,
                _mockScreenshotService,
                mockLogCollectionService);

            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert: Collect()が1回呼ばれていること
            Assert.AreEqual(1, mockLogCollectionService.CollectCalls.Count);

            // Assert: collectPathが evidence/collect 配下であること
            var (collectPath, targets) = mockLogCollectionService.CollectCalls[0];
            StringAssert.EndsWith(collectPath, Path.Combine("evidence", "collect"));
        }

        /// <summary>
        /// テスト観点: Stop()でのCollect()の呼び出しがExtractEvidence()の後であること
        /// （呼び出し順序はEvidenceExtractionのCallsが先に積まれていることで確認）
        /// </summary>
        [TestMethod]
        public void Stop_ShouldCallCollectAfterExtractEvidence()
        {
            // Arrange
            _mockConfigService.AppSettings = new AppSettings
            {
                EvidenceSavePath = "C:\\TempEvidence",
                KeepSnapshot = false,
                AutoSaveScreenshots = false
            };

            var callOrder = new List<string>();
            // ExtractEvidenceが呼ばれたらorderに記録するカスタムMock
            var orderTrackingExtractionService = new OrderTrackingExtractionService(callOrder);
            var orderTrackingCollectionService = new OrderTrackingCollectionService(callOrder);

            var monitoringService = new MonitoringService(
                _mockConfigService,
                _mockUserInteractionService,
                orderTrackingExtractionService,
                _mockScreenshotService,
                orderTrackingCollectionService);

            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert: ExtractEvidence → Collect の順
            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual("ExtractEvidence", callOrder[0]);
            Assert.AreEqual("Collect", callOrder[1]);
        }

        private class OrderTrackingExtractionService : IEvidenceExtractionService
        {
            private readonly List<string> _order;
            public OrderTrackingExtractionService(List<string> order) => _order = order;
            public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets) { }
            public bool ExtractEvidence(string s1, string s2, string ev, bool keep)
            {
                _order.Add("ExtractEvidence");
                return false;
            }
        }

        private class OrderTrackingCollectionService : ILogCollectionService
        {
            private readonly List<string> _order;
            public OrderTrackingCollectionService(List<string> order) => _order = order;
            public void Collect(string collectPath, IEnumerable<CollectTarget> targets)
            {
                _order.Add("Collect");
            }
        }
    }
}