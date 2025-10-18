using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvidenceSupportTool.Services;
using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class EvidenceExtractionServiceTests
    {
        private string _testRoot;
        private EvidenceExtractionService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "EESTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testRoot);
            _service = new EvidenceExtractionService();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        [TestMethod]
        public void CreateSnapshot_ShouldCopySingleFile()
        {
            // テストの観点: 単一のファイルが指定された場合に、スナップショットディレクトリに正しくコピーされること。

            // Arrange
            // テスト用のソースファイルを作成
            string sourceDir = Path.Combine(_testRoot, "source");
            Directory.CreateDirectory(sourceDir);
            string sourceFilePath = Path.Combine(sourceDir, "test.log");
            File.WriteAllText(sourceFilePath, "This is a test log.");

            // 監視対象を設定
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "AppLog", PathPattern = sourceFilePath }
            };

            // 出力先ディレクトリを設定
            string snapshotDir = Path.Combine(_testRoot, "snapshot");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            // 期待される出力パス
            string expectedDestDir = Path.Combine(snapshotDir, "AppLog");
            string expectedDestFile = Path.Combine(expectedDestDir, "test.log");

            Assert.IsTrue(Directory.Exists(expectedDestDir), "ターゲットごとのサブディレクトリが作成されていません。");
            Assert.IsTrue(File.Exists(expectedDestFile), "ファイルがコピーされていません。");
            Assert.AreEqual("This is a test log.", File.ReadAllText(expectedDestFile), "コピーされたファイルの内容が一致しません。");
        }

        [TestMethod]
        public void CreateSnapshot_ShouldResolveDateFormatsInPath()
        {
            // テストの観点: パス内の日付フォーマット({YYYY}, {MM}, {DD})が正しく解決され、ファイルがコピーされること。

            // Arrange
            // 今日の日付に基づいたパスとファイルを作成
            var today = DateTime.Now;
            string datePattern = $"{today:yyyy}{today:MM}{today:dd}";
            string sourceDir = Path.Combine(_testRoot, "source", datePattern);
            Directory.CreateDirectory(sourceDir);
            string sourceFilePath = Path.Combine(sourceDir, "date_test.log");
            File.WriteAllText(sourceFilePath, "Date format test.");

            // 監視対象のパスには日付フォーマットを含める
            string patternPath = Path.Combine(_testRoot, "source", "{YYYY}{MM}{DD}", "date_test.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "DateLog", PathPattern = patternPath }
            };

            string snapshotDir = Path.Combine(_testRoot, "snapshot");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            string expectedDestFile = Path.Combine(snapshotDir, "DateLog", "date_test.log");
            Assert.IsTrue(File.Exists(expectedDestFile), "日付フォーマットが解決されたファイルがコピーされていません。");
            Assert.AreEqual("Date format test.", File.ReadAllText(expectedDestFile));
        }

        [TestMethod]
        public void CreateSnapshot_ShouldResolveWildcardInPath()
        {
            // テストの観点: パス内のワイルドカード(*)が正しく解決され、該当する全てのファイルがコピーされること。

            // Arrange
            string sourceDir = Path.Combine(_testRoot, "source");
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "wildcard_a.log"), "Wildcard A");
            File.WriteAllText(Path.Combine(sourceDir, "wildcard_b.log"), "Wildcard B");
            File.WriteAllText(Path.Combine(sourceDir, "other.txt"), "Other file");

            string patternPath = Path.Combine(sourceDir, "wildcard_*.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "WildcardLog", PathPattern = patternPath }
            };

            string snapshotDir = Path.Combine(_testRoot, "snapshot");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            string expectedDestDir = Path.Combine(snapshotDir, "WildcardLog");
            string expectedFileA = Path.Combine(expectedDestDir, "wildcard_a.log");
            string expectedFileB = Path.Combine(expectedDestDir, "wildcard_b.log");
            string unexpectedFile = Path.Combine(expectedDestDir, "other.txt");

            Assert.IsTrue(File.Exists(expectedFileA), "ワイルドカードに一致するファイルAがコピーされていません。");
            Assert.AreEqual("Wildcard A", File.ReadAllText(expectedFileA));
            Assert.IsTrue(File.Exists(expectedFileB), "ワイルドカードに一致するファイルBがコピーされていません。");
            Assert.AreEqual("Wildcard B", File.ReadAllText(expectedFileB));
            Assert.IsFalse(File.Exists(unexpectedFile), "ワイルドカードに一致しないファイルがコピーされています。");
        }
    }
}
