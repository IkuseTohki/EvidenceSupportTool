using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography; // For MD5 hash

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IEvidenceExtractionServiceの実装クラスです。
    /// </summary>
    public class EvidenceExtractionService : IEvidenceExtractionService
    {
        private readonly IUserInteractionService _userInteractionService;

        public EvidenceExtractionService(IUserInteractionService userInteractionService)
        {
            _userInteractionService = userInteractionService;
        }

        public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
        {
            try
            {
                // スナップショットの親ディレクトリを作成
                Directory.CreateDirectory(snapshotPath);

                foreach (var target in targets)
                {
                    // パスパターンを解決 (日付フォーマット)
                    string resolvedPathPattern = ResolveDateFormats(target.PathPattern);

                    IEnumerable<string> filesToCopy;

                    if (resolvedPathPattern.Contains("*"))
                    {
                        // ワイルドカードを含む場合
                        string directory = Path.GetDirectoryName(resolvedPathPattern);
                        string searchPattern = Path.GetFileName(resolvedPathPattern);

                        if (Directory.Exists(directory))
                        {
                            filesToCopy = Directory.GetFiles(directory, searchPattern);
                        }
                        else
                        {
                            filesToCopy = Enumerable.Empty<string>(); // ディレクトリが存在しない場合は空のリストを返す
                        }
                    }
                    else
                    {
                        // ワイルドカードを含まない場合 (単一ファイル)
                        if (File.Exists(resolvedPathPattern))
                        {
                            filesToCopy = new List<string> { resolvedPathPattern };
                        }
                        else
                        {
                            filesToCopy = Enumerable.Empty<string>(); // ファイルが存在しない場合は空のリストを返す
                        }
                    }

                    foreach (string sourcePath in filesToCopy)
                    {
                        // ターゲットごとのサブディレクトリを作成
                        string targetDir = Path.Combine(snapshotPath, target.Name);
                        Directory.CreateDirectory(targetDir);

                        // ファイルをコピー
                        string destPath = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                        File.Copy(sourcePath, destPath, true); // trueで上書きを許可
                    }
                }
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"スナップショットの作成中にエラーが発生しました: {ex.Message}");
            }
        }

        public bool ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath, bool keepSnapshot)
        {
            bool hasDifference = false;
            try
            {
                // snapshot1とsnapshot2のファイルリストを相対パスで取得
                var snapshot1Files = GetRelativeFilePaths(snapshot1Path);
                var snapshot2Files = GetRelativeFilePaths(snapshot2Path);

                // 変更されたファイルと新規追加されたファイルを検出
                foreach (var file2 in snapshot2Files)
                {
                    string fullPath2 = Path.Combine(snapshot2Path, file2.Key);
                    string fullPath1 = Path.Combine(snapshot1Path, file2.Key);

                    if (snapshot1Files.ContainsKey(file2.Key))
                    {
                        // 両方に存在するファイル: 内容が変更されたかチェック
                        if (!CompareFilesByHash(fullPath1, fullPath2))
                        {
                            // 変更あり
                            CopyFileToEvidence(fullPath2, evidencePath, file2.Key);
                            hasDifference = true;
                        }
                    }
                    else
                    {
                        // snapshot2にのみ存在するファイル: 新規追加
                        CopyFileToEvidence(fullPath2, evidencePath, file2.Key);
                        hasDifference = true;
                    }
                }

                // スナップショットの削除
                if (!keepSnapshot)
                {
                    if (Directory.Exists(snapshot1Path))
                    {
                        Directory.Delete(snapshot1Path, true);
                    }
                    if (Directory.Exists(snapshot2Path))
                    {
                        Directory.Delete(snapshot2Path, true);
                    }
                }

                // snapshot1にのみ存在するファイル (削除されたファイル) はevidenceにはコピーしない

                return hasDifference;
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"エビデンスの抽出中にエラーが発生しました: {ex.Message}");
                return false; // エラー発生時は差分なしとして扱うか、別途エラー状態を返すか検討
            }
        }

        private string ResolveDateFormats(string pathPattern)
        {
            var today = DateTime.Now;
            return pathPattern.Replace("{YYYY}", today.ToString("yyyy"))
                                .Replace("{MM}", today.ToString("MM"))
                                .Replace("{DD}", today.ToString("dd"));
        }

        /// <summary>
        /// 指定されたディレクトリ内の全ファイルを相対パスとフルパスの辞書として取得します。
        /// </summary>
        private Dictionary<string, string> GetRelativeFilePaths(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定されたスナップショットディレクトリが見つかりません: {directoryPath}");
            }

            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .ToDictionary(
                                fullPath => Path.GetRelativePath(directoryPath, fullPath),
                                fullPath => fullPath
                            );
        }

        /// <summary>
        /// 2つのファイルのハッシュ値を比較して、内容が同じかどうかを判定します。
        /// </summary>
        private bool CompareFilesByHash(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                return false; // どちらかのファイルが存在しない場合は異なる
            }

            using (var md5 = MD5.Create())
            {
                using (var stream1 = File.OpenRead(filePath1))
                using (var stream2 = File.OpenRead(filePath2))
                {
                    var hash1 = md5.ComputeHash(stream1);
                    var hash2 = md5.ComputeHash(stream2);
                    return hash1.SequenceEqual(hash2);
                }
            }
        }

        /// <summary>
        /// ファイルをevidenceディレクトリにコピーします。
        /// </summary>
        private void CopyFileToEvidence(string sourceFullPath, string evidenceRootPath, string relativePath)
        {
            // 差分が検出された場合にのみevidencePath ディレクトリを作成
            if (!Directory.Exists(evidenceRootPath))
            {
                Directory.CreateDirectory(evidenceRootPath);
            }

            string destFullPath = Path.Combine(evidenceRootPath, relativePath);
            string destDir = Path.GetDirectoryName(destFullPath);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Copy(sourceFullPath, destFullPath, true);
        }
    }
}