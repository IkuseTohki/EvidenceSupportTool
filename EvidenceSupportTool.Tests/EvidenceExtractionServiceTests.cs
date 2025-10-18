using Microsoft.VisualStudio.TestTools.UnitTesting;
using EvidenceSupportTool.Services;
using EvidenceSupportTool.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions; // For Regex

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class EvidenceExtractionServiceTests
    {
        // IUserInteractionServiceのモッククラス
        private class MockUserInteractionService : IUserInteractionService
        {
            public List<string> ShowMessageCalls { get; } = new List<string>();
            public List<string> ShowErrorCalls { get; } = new List<string>();

            public void ShowMessage(string message) => ShowMessageCalls.Add(message);
            public void ShowError(string message) => ShowErrorCalls.Add(message);
        }

        private string _testRoot = null!;
        private EvidenceExtractionService _service = null!;
        private IUserInteractionService _mockUserInteractionService = null!; 

        [TestInitialize]
        public void TestInitialize()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "EESTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testRoot);
            _mockUserInteractionService = new MockUserInteractionService(); 
            _service = new EvidenceExtractionService(_mockUserInteractionService); // Modified
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
            Assert.AreEqual("This is a test log.", File.ReadAllText(expectedDestFile), "コピーされたファイルの内容が一致しません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
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
            Assert.IsTrue(File.Exists(expectedDestFile), "日付フォーマットが解決されたファイルがコピーされていません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
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

            Assert.IsTrue(File.Exists(expectedFileA), "ワイルドカードに一致するファイルAがコピーされていません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.AreEqual("Wildcard A", File.ReadAllText(expectedFileA));
            Assert.IsTrue(File.Exists(expectedFileB), "ワイルドカードに一致するファイルBがコピーされていません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.AreEqual("Wildcard B", File.ReadAllText(expectedFileB));
            Assert.IsFalse(File.Exists(unexpectedFile), "ワイルドカードに一致しないファイルがコピーされています。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
        }

        [TestMethod]
        public void ExtractEvidence_ShouldDetectChangesAndCopyDifferences()
        {
            // テストの観点: snapshot1とsnapshot2を比較し、snapshot1のファイルサイズ以降のデータがevidenceディレクトリにコピーされること。
            // 新規追加ファイルはevidenceディレクトリにコピーされること。
            // 差分がある場合はtrueを返すこと。

            // Arrange
            string snapshot1Dir = Path.Combine(_testRoot, "snapshot1");
            string snapshot2Dir = Path.Combine(_testRoot, "snapshot2");
            string evidenceDir = Path.Combine(_testRoot, "evidence");

            // snapshot1の準備
            Directory.CreateDirectory(Path.Combine(snapshot1Dir, "Target1"));
            File.WriteAllText(Path.Combine(snapshot1Dir, "Target1", "file1.log"), "Content A\n"); // 変更されるファイル (改行で終わる)
            Directory.CreateDirectory(Path.Combine(snapshot1Dir, "Target2"));
            File.WriteAllText(Path.Combine(snapshot1Dir, "Target2", "file2.log"), "Content B"); // 変更されないファイル
            Directory.CreateDirectory(Path.Combine(snapshot1Dir, "Target4"));
            File.WriteAllText(Path.Combine(snapshot1Dir, "Target4", "file4.log"), "Content D"); // 削除されるファイル

            // snapshot2の準備
            Directory.CreateDirectory(Path.Combine(snapshot2Dir, "Target1"));
            File.WriteAllText(Path.Combine(snapshot2Dir, "Target1", "file1.log"), "Content A\nContent A - Changed\n"); // 変更後 (追記)
            Directory.CreateDirectory(Path.Combine(snapshot2Dir, "Target2"));
            File.WriteAllText(Path.Combine(snapshot2Dir, "Target2", "file2.log"), "Content B"); // 変更なし
            Directory.CreateDirectory(Path.Combine(snapshot2Dir, "Target3"));
            File.WriteAllText(Path.Combine(snapshot2Dir, "Target3", "file3.log"), "Content C - New"); // 新規追加ファイル

            // Act
            bool hasDifference = _service.ExtractEvidence(snapshot1Dir, snapshot2Dir, evidenceDir, false); // Modified

            // Assert
            // 1. 差分があったことを検証
            Assert.IsTrue(hasDifference, "差分が検出されるべきです。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());

            // 2. 変更されたファイルの差分がコピーされていることを検証
            string changedFileInEvidence = Path.Combine(evidenceDir, "Target1", "file1.log");
            Assert.IsTrue(File.Exists(changedFileInEvidence), "変更されたファイルの差分がevidenceディレクトリにコピーされていません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.AreEqual("Content A - Changed\n", File.ReadAllText(changedFileInEvidence), "変更されたファイルの差分内容が一致しません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());

            // 3. 新規追加されたファイルがコピーされていることを検証
            string newFileInEvidence = Path.Combine(evidenceDir, "Target3", "file3.log");
            Assert.IsTrue(File.Exists(newFileInEvidence), "新規追加されたファイルがevidenceディレクトリにコピーされていません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.AreEqual("Content C - New", File.ReadAllText(newFileInEvidence), "新規追加されたファイルの内容が一致しません。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());

            // 4. 変更されていないファイルがコピーされていないことを検証
            string unchangedFileInEvidence = Path.Combine(evidenceDir, "Target2", "file2.log");
            Assert.IsFalse(File.Exists(unchangedFileInEvidence), "変更されていないファイルがevidenceディレクトリにコピーされています。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());

            // 5. 削除されたファイルがコピーされていないことを検証
            string deletedFileInEvidence = Path.Combine(evidenceDir, "Target4", "file4.log");
            Assert.IsFalse(File.Exists(deletedFileInEvidence), "削除されたファイルがevidenceディレクトリにコピーされています。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
        }

        [TestMethod]
        public void ExtractEvidence_ShouldReturnFalseWhenNoChanges()
        {
            // テストの観点: snapshot1とsnapshot2に差分がない場合、falseを返すこと。

            // Arrange
            string snapshot1Dir = Path.Combine(_testRoot, "snapshot1_nochange");
            string snapshot2Dir = Path.Combine(_testRoot, "snapshot2_nochange");
            string evidenceDir = Path.Combine(_testRoot, "evidence_nochange");

            // snapshot1の準備
            Directory.CreateDirectory(Path.Combine(snapshot1Dir, "Target1"));
            File.WriteAllText(Path.Combine(snapshot1Dir, "Target1", "file1.log"), "Content A");

            // snapshot2の準備 (snapshot1と全く同じ内容)
            Directory.CreateDirectory(Path.Combine(snapshot2Dir, "Target1"));
            File.WriteAllText(Path.Combine(snapshot2Dir, "Target1", "file1.log"), "Content A");

            // Act
            bool hasDifference = _service.ExtractEvidence(snapshot1Dir, snapshot2Dir, evidenceDir, false); // Modified

            // Assert
            Assert.IsFalse(hasDifference, "差分がない場合、falseが返されるべきです。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.IsFalse(Directory.Exists(evidenceDir), "差分がない場合、evidenceディレクトリは作成されないべきです。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
        }

        

        [TestMethod]
        public void ExtractEvidence_ShouldCallShowErrorOnException()
        {
            // テストの観点: ExtractEvidenceメソッド内でファイル操作エラーが発生した場合に、IUserInteractionService.ShowErrorが呼び出されること。

            // Arrange
            string invalidSnapshot1Path = "Z:\\non_existent_drive\\snapshot1"; // 存在しないドライブ
            string snapshot2Path = Path.Combine(_testRoot, "snapshot2");
            string evidencePath = Path.Combine(_testRoot, "evidence");

            // Act
            _service.ExtractEvidence(invalidSnapshot1Path, snapshot2Path, evidencePath, false); // Add keepSnapshot

            // Assert
            Assert.AreEqual(1, ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.Count, "エラー発生時にShowErrorが呼び出されていません。");
            StringAssert.Contains(((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls[0], "エビデンスの抽出中にエラーが発生しました");
        }

        [TestMethod]
        public void ExtractEvidence_ShouldDeleteSnapshotsWhenKeepSnapshotIsFalse()
        {
            // テストの観点: KeepSnapshotがfalseの場合、ExtractEvidence実行後にsnapshot1とsnapshot2ディレクトリが削除されること。

            // Arrange
            string snapshot1Dir = Path.Combine(_testRoot, "snapshot1_delete");
            string snapshot2Dir = Path.Combine(_testRoot, "snapshot2_delete");
            string evidenceDir = Path.Combine(_testRoot, "evidence_delete");

            Directory.CreateDirectory(snapshot1Dir);
            File.WriteAllText(Path.Combine(snapshot1Dir, "file.log"), "content");
            Directory.CreateDirectory(snapshot2Dir);
            File.WriteAllText(Path.Combine(snapshot2Dir, "file.log"), "content");

            // Act
            _service.ExtractEvidence(snapshot1Dir, snapshot2Dir, evidenceDir, false); // KeepSnapshot = false

            // Assert
            Assert.IsFalse(Directory.Exists(snapshot1Dir), "KeepSnapshotがfalseの場合、snapshot1が削除されていません。");
            Assert.IsFalse(Directory.Exists(snapshot2Dir), "KeepSnapshotがfalseの場合、snapshot2が削除されていません。");
        }

        [TestMethod]
        public void ExtractEvidence_ShouldNotDeleteSnapshotsWhenKeepSnapshotIsTrue()
        {
            // テストの観点: KeepSnapshotがtrueの場合、ExtractEvidence実行後にsnapshot1とsnapshot2ディレクトリが残ること。

            // Arrange
            string snapshot1Dir = Path.Combine(_testRoot, "snapshot1_keep");
            string snapshot2Dir = Path.Combine(_testRoot, "snapshot2_keep");
            string evidenceDir = Path.Combine(_testRoot, "evidence_keep");

            Directory.CreateDirectory(snapshot1Dir);
            File.WriteAllText(Path.Combine(snapshot1Dir, "file.log"), "content");
            Directory.CreateDirectory(snapshot2Dir);
            File.WriteAllText(Path.Combine(snapshot2Dir, "file.log"), "content");

            // Act
            _service.ExtractEvidence(snapshot1Dir, snapshot2Dir, evidenceDir, true); // KeepSnapshot = true

            // Assert
            Assert.IsTrue(Directory.Exists(snapshot1Dir), "KeepSnapshotがtrueの場合、snapshot1が削除されています。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
            Assert.IsTrue(Directory.Exists(snapshot2Dir), "KeepSnapshotがtrueの場合、snapshot2が削除されています。エラーメッセージ: " + ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.FirstOrDefault());
        }

        [TestMethod]
        public void ExtractEvidence_ShouldDetectLineAddedToFile()
        {
            // テストの観点: ファイルに行が追加された場合に差分が検出され、evidenceディレクトリにコピーされること。

            // Arrange
            string snapshot1Dir = Path.Combine(_testRoot, "snapshot1_line_added");
            string snapshot2Dir = Path.Combine(_testRoot, "snapshot2_line_added");
            string evidenceDir = Path.Combine(_testRoot, "evidence_line_added");

            // snapshot1の準備
            Directory.CreateDirectory(Path.Combine(snapshot1Dir, "Target1"));
            string file1Path = Path.Combine(snapshot1Dir, "Target1", "log.txt");
            File.WriteAllText(file1Path, "Line 1\nLine 2");

            // snapshot2の準備 (行を追加)
            Directory.CreateDirectory(Path.Combine(snapshot2Dir, "Target1"));
            string file2Path = Path.Combine(snapshot2Dir, "Target1", "log.txt");
            File.WriteAllText(file2Path, "Line 1\nLine 2\nLine 3");

            // Act
            bool hasDifference = _service.ExtractEvidence(snapshot1Dir, snapshot2Dir, evidenceDir, false);

            // Assert
            Assert.IsTrue(hasDifference, "行が追加されたファイルで差分が検出されるべきです。");
            string evidenceFilePath = Path.Combine(evidenceDir, "Target1", "log.txt");
            Assert.IsTrue(File.Exists(evidenceFilePath), "変更されたファイルがevidenceディレクトリにコピーされていません。");
            Assert.AreEqual("\nLine 3", File.ReadAllText(evidenceFilePath), "コピーされたファイルの内容が一致しません。");
        }

        [TestMethod]
        public void CreateSnapshot_ShouldNotThrowExceptionWhenFileNotFound()
        {
            // テストの観点: 存在しない単一ファイルを指定した場合に例外がスローされず、ShowErrorも呼び出されないこと。

            // Arrange
            string nonExistentFilePath = Path.Combine(_testRoot, "non_existent.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "NonExistentLog", PathPattern = nonExistentFilePath }
            };
            string snapshotDir = Path.Combine(_testRoot, "snapshot");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            Assert.AreEqual(0, ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.Count, "ファイルが見つからない場合にShowErrorが呼び出されました。");
            Assert.IsFalse(Directory.Exists(Path.Combine(snapshotDir, "NonExistentLog")), "存在しないファイルのスナップショットディレクトリが作成されました。");
        }

        [TestMethod]
        public void CreateSnapshot_ShouldNotThrowExceptionWhenDirectoryNotFound()
        {
            // テストの観点: 存在しないディレクトリ内のワイルドカードパスを指定した場合に例外がスローされず、ShowErrorも呼び出されないこと。

            // Arrange
            string nonExistentDirPath = Path.Combine(_testRoot, "non_existent_dir");
            string nonExistentWildcardPath = Path.Combine(nonExistentDirPath, "*.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "NonExistentDirLog", PathPattern = nonExistentWildcardPath }
            };
            string snapshotDir = Path.Combine(_testRoot, "snapshot");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            Assert.AreEqual(0, ((MockUserInteractionService)_mockUserInteractionService).ShowErrorCalls.Count, "ディレクトリが見つからない場合にShowErrorが呼び出されました。");
            Assert.IsFalse(Directory.Exists(Path.Combine(snapshotDir, "NonExistentDirLog")), "存在しないディレクトリのスナップショットディレクトリが作成されました。");
        }

        [TestMethod]
        public void CreateSnapshot_ShouldNotCopyAnyFileWhenFileNotFound()
        {
            // テストの観点: 存在しない単一ファイルを指定した場合に、スナップショットディレクトリに何もコピーされないこと。

            // Arrange
            string nonExistentFilePath = Path.Combine(_testRoot, "non_existent_file_to_copy.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "NonExistentFileTarget", PathPattern = nonExistentFilePath }
            };
            string snapshotDir = Path.Combine(_testRoot, "snapshot_no_copy_file");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            string targetSnapshotDir = Path.Combine(snapshotDir, "NonExistentFileTarget");
            Assert.IsFalse(Directory.Exists(targetSnapshotDir), "存在しないファイルのスナップショットディレクトリが作成されました。");
        }

        [TestMethod]
        public void CreateSnapshot_ShouldNotCopyAnyFileWhenDirectoryNotFound()
        {
            // テストの観点: 存在しないディレクトリ内のワイルドカードパスを指定した場合に、スナップショットディレクトリに何もコピーされないこと。

            // Arrange
            string nonExistentDirPath = Path.Combine(_testRoot, "non_existent_dir_to_copy");
            string nonExistentWildcardPath = Path.Combine(nonExistentDirPath, "*.log");
            var targets = new List<MonitoringTarget>
            {
                new MonitoringTarget { Name = "NonExistentDirTarget", PathPattern = nonExistentWildcardPath }
            };
            string snapshotDir = Path.Combine(_testRoot, "snapshot_no_copy_dir");

            // Act
            _service.CreateSnapshot(snapshotDir, targets);

            // Assert
            string targetSnapshotDir = Path.Combine(snapshotDir, "NonExistentDirTarget");
            Assert.IsFalse(Directory.Exists(targetSnapshotDir), "存在しないディレクトリのスナップショットディレクトリが作成されました。");
        }
    }
}