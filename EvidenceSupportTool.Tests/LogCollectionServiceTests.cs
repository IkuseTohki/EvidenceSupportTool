using EvidenceSupportTool.Models;
using EvidenceSupportTool.Services;
using System;
using System.IO;
using System.Linq;

namespace EvidenceSupportTool.Tests
{
    /// <summary>
    /// LogCollectionServiceのテストクラス。
    /// </summary>
    [TestClass]
    public class LogCollectionServiceTests
    {
        // IUserInteractionServiceのモッククラス
        private class MockUserInteractionService : IUserInteractionService
        {
            public System.Collections.Generic.List<string> ShowMessageCalls { get; } = new();
            public System.Collections.Generic.List<string> ShowErrorCalls { get; } = new();

            public void ShowMessage(string message) => ShowMessageCalls.Add(message);
            public void ShowError(string message) => ShowErrorCalls.Add(message);
        }

        private string _testRoot = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "LogCollectionServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        /// <summary>
        /// テスト観点: ワイルドカードなしパスで、既存ファイルが collectPath/{key}/ にコピーされること
        /// </summary>
        [TestMethod]
        public void Collect_SingleFile_ShouldCopyToTargetSubfolder()
        {
            // Arrange
            var sourceDir = Path.Combine(_testRoot, "source");
            Directory.CreateDirectory(sourceDir);
            var sourceFile = Path.Combine(sourceDir, "app.log");
            File.WriteAllText(sourceFile, "log content");

            var collectPath = Path.Combine(_testRoot, "collect");
            var target = new CollectTarget { Name = "AppLog", PathPattern = sourceFile };

            var service = new LogCollectionService(new MockUserInteractionService());

            // Act
            service.Collect(collectPath, new[] { target });

            // Assert
            var expectedFile = Path.Combine(collectPath, "AppLog", "app.log");
            Assert.IsTrue(File.Exists(expectedFile), $"コピー先ファイルが存在しません: {expectedFile}");
            Assert.AreEqual("log content", File.ReadAllText(expectedFile));
        }

        /// <summary>
        /// テスト観点: ワイルドカードありパスで、マッチしたファイル群がコピーされること
        /// </summary>
        [TestMethod]
        public void Collect_WildcardPattern_ShouldCopyMatchingFiles()
        {
            // Arrange
            var sourceDir = Path.Combine(_testRoot, "logs");
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "app_001.log"), "log1");
            File.WriteAllText(Path.Combine(sourceDir, "app_002.log"), "log2");
            File.WriteAllText(Path.Combine(sourceDir, "other.txt"), "other");

            var collectPath = Path.Combine(_testRoot, "collect");
            var pattern = Path.Combine(sourceDir, "*.log");
            var target = new CollectTarget { Name = "IisLog", PathPattern = pattern };

            var service = new LogCollectionService(new MockUserInteractionService());

            // Act
            service.Collect(collectPath, new[] { target });

            // Assert
            var iisLogDir = Path.Combine(collectPath, "IisLog");
            Assert.IsTrue(File.Exists(Path.Combine(iisLogDir, "app_001.log")), "app_001.log がコピーされていません");
            Assert.IsTrue(File.Exists(Path.Combine(iisLogDir, "app_002.log")), "app_002.log がコピーされていません");
            Assert.IsFalse(File.Exists(Path.Combine(iisLogDir, "other.txt")), "*.log 以外のファイルはコピーされてはいけません");
        }

        /// <summary>
        /// テスト観点: 対象ファイルが存在しない場合でも例外なく完了すること（空振り）
        /// </summary>
        [TestMethod]
        public void Collect_FileNotExists_ShouldCompleteWithoutException()
        {
            // Arrange
            var collectPath = Path.Combine(_testRoot, "collect");
            var target = new CollectTarget
            {
                Name = "MissingLog",
                PathPattern = Path.Combine(_testRoot, "nonexistent", "missing.log")
            };
            var service = new LogCollectionService(new MockUserInteractionService());

            // Act & Assert (例外が発生しないこと)
            service.Collect(collectPath, new[] { target });

            // collectPathは作成されていないはず（ファイルが0件）
            var targetDir = Path.Combine(collectPath, "MissingLog");
            Assert.IsFalse(Directory.Exists(targetDir), "ファイルが存在しない場合、サブフォルダが作成されてはいけません");
        }

        /// <summary>
        /// テスト観点: 日付フォーマット（{YYYY}, {MM}, {DD}）が実行時日付で解決されること
        /// </summary>
        [TestMethod]
        public void Collect_DateFormatPattern_ShouldResolveDateCorrectly()
        {
            // Arrange
            var now = DateTime.Now;
            var dateSubDir = Path.Combine(_testRoot, "logs", now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
            Directory.CreateDirectory(dateSubDir);
            File.WriteAllText(Path.Combine(dateSubDir, "dated.log"), "dated log");

            var collectPath = Path.Combine(_testRoot, "collect");
            var pattern = Path.Combine(_testRoot, "logs", "{YYYY}", "{MM}", "{DD}", "dated.log");
            var target = new CollectTarget { Name = "DatedLog", PathPattern = pattern };

            var service = new LogCollectionService(new MockUserInteractionService());

            // Act
            service.Collect(collectPath, new[] { target });

            // Assert
            var expectedFile = Path.Combine(collectPath, "DatedLog", "dated.log");
            Assert.IsTrue(File.Exists(expectedFile), $"日付解決後のファイルがコピーされていません: {expectedFile}");
        }

        /// <summary>
        /// テスト観点: 複数ターゲットが指定された場合、それぞれのサブフォルダに独立してコピーされること
        /// </summary>
        [TestMethod]
        public void Collect_MultipleTargets_ShouldCopyToEachSubfolder()
        {
            // Arrange
            var sourceDir1 = Path.Combine(_testRoot, "src1");
            var sourceDir2 = Path.Combine(_testRoot, "src2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);
            File.WriteAllText(Path.Combine(sourceDir1, "a.log"), "log a");
            File.WriteAllText(Path.Combine(sourceDir2, "b.log"), "log b");

            var collectPath = Path.Combine(_testRoot, "collect");
            var targets = new[]
            {
                new CollectTarget { Name = "TargetA", PathPattern = Path.Combine(sourceDir1, "a.log") },
                new CollectTarget { Name = "TargetB", PathPattern = Path.Combine(sourceDir2, "b.log") }
            };
            var service = new LogCollectionService(new MockUserInteractionService());

            // Act
            service.Collect(collectPath, targets);

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(collectPath, "TargetA", "a.log")));
            Assert.IsTrue(File.Exists(Path.Combine(collectPath, "TargetB", "b.log")));
        }
    }
}
