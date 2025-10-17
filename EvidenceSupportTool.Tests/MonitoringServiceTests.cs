using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvidenceSupportTool.Services;
using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.Linq; // For .Count()

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class MonitoringServiceTests
    {
        [TestMethod]
        public void AddMonitoringTarget_ShouldAddTargetToList()
        {
            // テストの観点: MonitoringTargetが正しく追加されること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();
            MonitoringTarget target = new MonitoringTarget { Name = "TestTarget", PathPattern = "C:\\test\\file.txt" };

            // Act
            monitoringService.AddMonitoringTarget(target);

            // Assert
            var targets = monitoringService.GetMonitoringTargets();
            Assert.IsNotNull(targets);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(target, targets.First()); // .First()を使用するためにSystem.Linqを追加
        }

        [TestMethod]
        public void GetMonitoringTargets_ShouldReturnEmptyListInitially()
        {
            // テストの観点: 初期状態で監視対象リストが空であること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();

            // Act
            var targets = monitoringService.GetMonitoringTargets();

            // Assert
            Assert.IsNotNull(targets);
            Assert.AreEqual(0, targets.Count);
        }

        [TestMethod]
        public void StartMonitoring_ShouldSetIsMonitoringActiveToTrue()
        {
            // テストの観点: 監視が開始されること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();
            Assert.IsFalse(monitoringService.IsMonitoringActive()); // 初期状態が非アクティブであることを確認

            // Act
            monitoringService.StartMonitoring();

            // Assert
            Assert.IsTrue(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void StopMonitoring_ShouldSetIsMonitoringActiveToFalse()
        {
            // テストの観点: 監視が停止されること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();
            monitoringService.StartMonitoring(); // 監視を開始した状態にする
            Assert.IsTrue(monitoringService.IsMonitoringActive()); // 開始されていることを確認

            // Act
            monitoringService.StopMonitoring();

            // Assert
            Assert.IsFalse(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void IsMonitoringActive_ShouldReturnFalseInitially()
        {
            // テストの観点: 初期状態で監視がアクティブでないこと
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();

            // Act & Assert
            Assert.IsFalse(monitoringService.IsMonitoringActive());
        }

        [TestMethod]
        public void RemoveMonitoringTarget_ShouldRemoveExistingTarget()
        {
            // テストの観点: 既存の監視対象が正しく削除されること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();
            MonitoringTarget target1 = new MonitoringTarget { Name = "Target1", PathPattern = "C:\\path1" };
            MonitoringTarget target2 = new MonitoringTarget { Name = "Target2", PathPattern = "C:\\path2" };
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
            // テストの観点: 存在しない監視対象の削除が失敗すること
            // Arrange
            IMonitoringService monitoringService = new MonitoringService();
            MonitoringTarget target1 = new MonitoringTarget { Name = "Target1", PathPattern = "C:\\path1" };
            monitoringService.AddMonitoringTarget(target1);
            Assert.AreEqual(1, monitoringService.GetMonitoringTargets().Count);

            // Act
            bool result = monitoringService.RemoveMonitoringTarget("NonExistentTarget");

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, monitoringService.GetMonitoringTargets().Count);
            Assert.IsTrue(monitoringService.GetMonitoringTargets().Any(t => t.Name == "Target1"));
        }
    }
}