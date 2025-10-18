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

            public AppSettings GetAppSettings() => AppSettings;
            public IEnumerable<MonitoringTarget> GetMonitoringTargets() => MonitoringTargets;
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
            public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
            {
                // 何もしない
            }

            public void ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)
            {
                // 何もしない
            }
        }

        private MockConfigService _mockConfigService;
        private MockUserInteractionService _mockUserInteractionService;
        private MockEvidenceExtractionService _mockEvidenceExtractionService;
        private List<string> _receivedStatuses;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfigService = new MockConfigService();
            _mockUserInteractionService = new MockUserInteractionService();
            _mockEvidenceExtractionService = new MockEvidenceExtractionService();
            _receivedStatuses = new List<string>();
        }

        [TestMethod]
        public void AddMonitoringTarget_ShouldAddTargetToList()
        {
            // テストの観点: 監視対象がリストに正しく追加されること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);

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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);

            // Act & Assert
            Assert.IsFalse(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void RemoveMonitoringTarget_ShouldRemoveExistingTarget()
        {
            // テストの観点: 登録済みの監視対象が、名前を指定して正しく削除できること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
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

            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);

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
            // テストの観点: 差分がない場合に、その旨がユーザーに通知されること。
            // Arrange
            var monitoringService = new MonitoringService(_mockConfigService, _mockUserInteractionService, _mockEvidenceExtractionService);
            monitoringService.Start();

            // Act
            monitoringService.Stop();

            // Assert
            Assert.AreEqual(1, _mockUserInteractionService.ShowMessageCalls.Count);
            Assert.AreEqual("差分はありませんでした。", _mockUserInteractionService.ShowMessageCalls[0]);
        }
    }
}